using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace X99D3M4FanInit;

internal static class Program
{
    private const string Version = "1.2.0";
    private const ushort ExpectedVendorId = 0x5CA3;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        bool unattended = args.Contains("--unattended", StringComparer.OrdinalIgnoreCase);
        if (args.Any(a => a is "--help" or "-h" or "/?"))
        {
            Console.WriteLine("X99D3M4FanInit [--unattended]\n默认要求输入 YES；计划任务使用 --unattended。所有安全校验始终启用。");
            return 0;
        }
        if (args.Any(a => !a.Equals("--unattended", StringComparison.OrdinalIgnoreCase)))
            return Finish("未知参数。使用 --help 查看帮助。", 2);
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitOperatingSystem)
            return Finish("仅支持 64 位 Windows。", 2);
        if (!IsAdministrator())
            return Finish("需要管理员权限。", 3);

        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "X99D3M4FanInit", "logs");
        Directory.CreateDirectory(logDir);
        string logPath = Path.Combine(logDir, $"init-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var log = new List<string> { $"X99D3M4FanInit {Version}", $"Time: {DateTimeOffset.Now:O}", $"Computer: {Environment.MachineName}" };
        int exitCode;
        string result;
        try
        {
            (result, exitCode) = EnsurePwm(unattended, log);
        }
        catch (Exception ex)
        {
            result = "初始化失败：" + ex.Message;
            log.Add(ex.ToString());
            exitCode = 10;
        }
        log.Add($"Result: {result}");
        log.Add($"ExitCode: {exitCode}");
        File.WriteAllLines(logPath, log, new UTF8Encoding(false));
        Console.WriteLine(result);
        Console.WriteLine("日志：" + logPath);
        return exitCode;
    }

    private static (string, int) EnsurePwm(bool unattended, List<string> log)
    {
        using var io = new HardwareIo();
        io.Open();
        if (!io.AcquireIsa(5000))
            throw new InvalidOperationException("无法取得 ISA 总线锁；请退出其他硬件监控软件后重试。");
        try
        {
            Chip? target = null;
            foreach (ushort port in new ushort[] { 0x2E, 0x4E })
            {
                Chip chip = Probe(io, port);
                log.Add($"Probe 0x{port:X2}: ID=0x{chip.Id:X2} REV=0x{chip.Revision:X2} HWM=0x{chip.Base:X4} Valid={chip.Valid}");
                if (IsTarget(chip) && chip.Valid) target = chip;
            }
            if (target is null)
                return ("安全拒绝：未检测到具有有效监控基址的 NCT5532D/C56x。未写入。", 4);

            io.SelectConfigPort(target.Port);
            ushort indexPort = (ushort)(target.Base + 5);
            ushort dataPort = (ushort)(target.Base + 6);
            byte originalBank = CurrentBank(io, indexPort, dataPort);
            try
            {
                byte vendorHigh = ReadBanked(io, indexPort, dataPort, 0x80, 0x4F);
                byte vendorLow = ReadBanked(io, indexPort, dataPort, 0, 0x4F);
                ushort vendor = (ushort)((vendorHigh << 8) | vendorLow);
                log.Add($"VendorID: 0x{vendor:X4}");
                if (vendor != ExpectedVendorId)
                    return ($"安全拒绝：Vendor ID 为 0x{vendor:X4}，预期 0x{ExpectedVendorId:X4}。未写入。", 5);

                byte original = ReadBanked(io, indexPort, dataPort, 0, 0x04);
                byte requested = (byte)(original & 0xFE);
                log.Add($"Bank0:04 before: 0x{original:X2}");
                if (original == requested)
                    return ("SYS_FAN 已处于 PWM 模式，无需写入。", 0);
                if (!unattended)
                {
                    Console.WriteLine($"即将把 Bank0:04 从 0x{original:X2} 改为 0x{requested:X2}（仅清除 bit0）。");
                    Console.Write("输入 YES 确认：");
                    if (!string.Equals(Console.ReadLine()?.Trim(), "YES", StringComparison.Ordinal))
                        return ("用户取消，未写入。", 6);
                }

                WriteBanked(io, indexPort, dataPort, 0, 0x04, requested);
                byte verify = ReadBanked(io, indexPort, dataPort, 0, 0x04);
                log.Add($"Bank0:04 write: 0x{original:X2} -> 0x{requested:X2}; verify=0x{verify:X2}");
                if (verify != requested)
                    throw new InvalidOperationException($"写后回读失败，实际值 0x{verify:X2}。");
                return ("SYS_FAN 已成功切换为 PWM 模式。", 0);
            }
            finally { SelectBank(io, indexPort, dataPort, originalBank); }
        }
        finally { io.ReleaseIsa(); }
    }

    private static Chip Probe(HardwareIo io, ushort port)
    {
        ushort data = (ushort)(port + 1);
        byte? oldLdn = null;
        io.SelectConfigPort(port);
        io.WritePort(port, 0x87); io.WritePort(port, 0x87);
        try
        {
            byte id = ReadConfig(io, port, data, 0x20);
            byte rev = ReadConfig(io, port, data, 0x21);
            if (id is 0 or 0xFF) return new(port, id, rev, 0, false);
            io.FindBars();
            oldLdn = ReadConfig(io, port, data, 0x07);
            WriteConfig(io, port, data, 0x07, 0x0B);
            ushort b1 = (ushort)((ReadConfig(io, port, data, 0x60) << 8) | ReadConfig(io, port, data, 0x61));
            Thread.Sleep(1);
            ushort b2 = (ushort)((ReadConfig(io, port, data, 0x60) << 8) | ReadConfig(io, port, data, 0x61));
            return new(port, id, rev, b1, b1 == b2 && b1 >= 0x100 && (b1 & 0xF007) == 0);
        }
        finally
        {
            if (oldLdn.HasValue) WriteConfig(io, port, data, 0x07, oldLdn.Value);
            io.WritePort(port, 0xAA);
        }
    }

    private static bool IsTarget(Chip c) => c.Id == 0xC5 && (c.Revision & 0xF0) == 0x60;
    private static byte ReadBanked(HardwareIo io, ushort i, ushort d, byte bank, byte index) { SelectBank(io, i, d, bank); io.WritePort(i, index); return io.ReadPort(d); }
    private static void WriteBanked(HardwareIo io, ushort i, ushort d, byte bank, byte index, byte value) { SelectBank(io, i, d, bank); io.WritePort(i, index); io.WritePort(d, value); }
    private static byte CurrentBank(HardwareIo io, ushort i, ushort d) { io.WritePort(i, 0x4E); return io.ReadPort(d); }
    private static void SelectBank(HardwareIo io, ushort i, ushort d, byte bank) { io.WritePort(i, 0x4E); io.WritePort(d, bank); }
    private static byte ReadConfig(HardwareIo io, ushort i, ushort d, byte index) { io.WritePort(i, index); return io.ReadPort(d); }
    private static void WriteConfig(HardwareIo io, ushort i, ushort d, byte index, byte value) { io.WritePort(i, index); io.WritePort(d, value); }
    private static bool IsAdministrator() { using WindowsIdentity id = WindowsIdentity.GetCurrent(); return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); }
    private static int Finish(string message, int code) { Console.WriteLine(message); return code; }
    private sealed record Chip(ushort Port, byte Id, byte Revision, ushort Base, bool Valid);
}

internal sealed class HardwareIo : IDisposable
{
    private readonly Type _lpcPortType = typeof(Computer).Assembly.GetType("LibreHardwareMonitor.Hardware.Motherboard.Lpc.LpcPort", true)!;
    private readonly Type _mutexes = typeof(Computer).Assembly.GetType("LibreHardwareMonitor.Hardware.Mutexes", true)!;
    private readonly Dictionary<ushort, object> _ports = [];
    private object? _activePort;
    private bool _open, _locked;
    public void Open() { Invoke(_mutexes, "Open"); _open = true; }
    public bool AcquireIsa(int timeoutMs) { _locked = (bool)(Invoke(_mutexes, "WaitIsaBus", timeoutMs) ?? false); return _locked; }
    public void ReleaseIsa() { if (_locked) { Invoke(_mutexes, "ReleaseIsaBus"); _locked = false; } }
    public void SelectConfigPort(ushort configPort)
    {
        if (!_ports.TryGetValue(configPort, out object? port))
        {
            try
            {
                port = Activator.CreateInstance(_lpcPortType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { configPort, (ushort)(configPort + 1) }, CultureInfo.InvariantCulture)
                    ?? throw new InvalidOperationException("无法创建 LpcPort。");
                _ports.Add(configPort, port);
            }
            catch (Exception ex) { throw new InvalidOperationException("PawnIO LPC 访问不可用。请先通过可信的新版 FanControl 安装 PawnIO。", ex); }
        }
        _activePort = port;
    }
    public byte ReadPort(uint port) => (byte)(InvokeInstance(ActivePort, "ReadIoPort", checked((ushort)port)) ?? (byte)0);
    public void WritePort(uint port, byte value) => InvokeInstance(ActivePort, "WriteIoPort", checked((ushort)port), value);
    public void FindBars() => InvokeInstance(ActivePort, "FindBars");
    public void Dispose() { ReleaseIsa(); if (_open) foreach (object p in _ports.Values) InvokeInstance(p, "Close"); Invoke(_mutexes, "Close"); }
    private object ActivePort => _activePort ?? throw new InvalidOperationException("尚未选择配置端口。");
    private static object? Invoke(Type t, string n, params object[] a) { MethodInfo m = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Single(x => x.Name == n && x.GetParameters().Length == a.Length); try { return m.Invoke(null, a); } catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; } }
    private static object? InvokeInstance(object o, string n, params object[] a) { MethodInfo m = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(x => x.Name == n && x.GetParameters().Length == a.Length); try { return m.Invoke(o, a); } catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; } }
}
