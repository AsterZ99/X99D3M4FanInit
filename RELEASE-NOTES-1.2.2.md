# X99D3M4FanInit 1.2.2

Scheduled-task compatibility and verification fix.

- Creates the startup task with the Windows built-in `schtasks.exe` instead of the ScheduledTasks PowerShell module;
- Queries the task immediately after creation and treats a missing task as an installation failure;
- Starts the verified task once after installation;
- `Check.cmd` now uses `schtasks.exe` as the authoritative task lookup;
- No change to the PWM register operation or hardware safety checks.

Install v1.2.2 over the extracted older version, then run `Check.cmd`. A successful installation must show the task and a recent log.
