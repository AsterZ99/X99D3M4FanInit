# X99D3M4FanInit

一个面向 **科脑 X99D3M4 v1.11** 和 **Nuvoton NCT5532D/C56x** 的 Windows 启动初始化器。它在每次开机时将 `SYS_FAN` 输出类型安全地设为 PWM，让 FanControl 可以独立调节四线 PWM 风扇，不再需要先启动 SpeedFan。

> 这是社区工具，并非科脑、Nuvoton、FanControl 或 LibreHardwareMonitor 的官方项目。修改 Super I/O 寄存器存在风险，请先阅读安全边界。

## 解决了什么问题

在已测试的 X99D3M4 v1.11 上，BIOS 会把 NCT5532D 的 `SYSFANOUT` 初始化为 DC。FanControl/LibreHardwareMonitor 能设置控制值，但不会替用户决定三线 DC 与四线 PWM 的电气输出类型。因此直接启动 FanControl 时 SYS_FAN 无法按预期调速；在 SpeedFan 中把 **PWM Type** 从 **DC** 改为 **PWM** 后，FanControl 才能正常控制，重启后还可能需要重复。

本工具只替代 SpeedFan 完成这一步，不接管温控曲线，也不常驻后台。

## 解决思路

NCT5532D 使用 `Bank 0 / Index 0x04 / bit 0` 选择 SYSFANOUT 类型：

```text
bit0 = 1  -> DC 输出
bit0 = 0  -> PWM 输出
```

程序取得 ISA 总线锁，探测 Super I/O，验证 C56x、稳定 HWM 基址和 Nuvoton Vendor ID `0x5CA3`，然后读取 `Bank0:04`，仅清除 bit0，回读验证、写日志并退出。若已经是 PWM，不会重复写入。计划任务负责开机初始化，FanControl 继续负责占空比和温控曲线。

## 安装

前置条件：64 位 Windows 10/11；目标主板/芯片；已安装 PawnIO LPC 支持（推荐通过可信的新版 FanControl/PawnIO 安装）；禁用 SpeedFan 自启动。

1. 从 Releases 下载 `X99D3M4FanInit-<版本>-win-x64.zip`；
2. 完整解压，不能只复制 exe；
3. 双击 `Install.cmd` 并接受管理员权限提示；
4. 等几秒后双击 `Check.cmd`；返回代码为 `0`，日志显示“已成功切换”或“已经是 PWM”即成功；
5. 保持 FanControl 在用户登录后启动。

程序安装到 `%ProgramData%\X99D3M4FanInit`。任务名为 `X99D3M4 SYS_FAN PWM Initializer`，由 SYSTEM 在开机 15 秒后运行一次，失败时最多重试三次。

- `Run-Once.cmd`：交互执行一次，写入前要求输入大写 `YES`；
- `Check.cmd`：查看任务状态、返回代码和最新日志；
- `Uninstall.cmd`：删除任务；为保留日志，不自动删除安装目录。

日志位于 `%ProgramData%\X99D3M4FanInit\logs`。

## 安全边界

程序没有任意地址/值写入接口，只会清除 `Bank0:04 bit0`。芯片、HWM 基址、Vendor ID、ISA 锁或回读任一校验失败都会拒绝写入。`--unattended` 只省略人工确认，不绕过安全校验。工具不修改占空比、不处理 Tach/RPM，也不包含旧式 WinRing0 驱动。

不同主板即使采用相似芯片，也可能连接三线 DC 风扇。不要用于未经确认的其他主板。提交日志前可删除其中不希望公开的计算机名。

## 从源码构建

需要 .NET 8 SDK：

```powershell
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true
```

依赖固定为 `LibreHardwareMonitorLib 0.9.6`。自包含包无需目标电脑安装 .NET，所以约 36 MB。

## 许可与贡献

项目代码使用 [MIT License](LICENSE.txt)。LibreHardwareMonitorLib 使用 MPL-2.0，详见 [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)。支持其他主板必须有数据手册和实机证据，不应只凭芯片型号扩大自动写入范围。
