# X99D3M4FanInit 1.2.0

首个独立开源版本。

- 安全地将 X99D3M4 v1.11 / NCT5532D(C56x) SYSFANOUT 从 DC 切换为 PWM；
- 一键安装开机任务、检查状态及卸载；
- 验证芯片、HWM 基址、Vendor ID、ISA 锁及写后回读；
- 已是 PWM 时不写入；
- 自包含 Windows x64 包，无需另外安装 .NET。

首次安装后请运行 `Check.cmd`，确认返回代码为 0。本版本只解决 PWM 输出类型初始化，不解决 SYS_FAN Tach/RPM 读取问题。
