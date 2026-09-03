# X99D3M4FanInit 1.2.3

Log display and documentation update.

- `Check.ps1` now reads diagnostic logs explicitly as UTF-8, preventing Chinese result text from appearing garbled in Windows PowerShell 5.1;
- README now documents the expected successful task status, exit code, chip/vendor checks and the meaning of `Bank0:04 before: 0x00`;
- README records the false-negative task check in older releases and the v1.2.2 log-display limitation;
- No change to scheduled-task creation, PWM register operation or hardware safety checks.
