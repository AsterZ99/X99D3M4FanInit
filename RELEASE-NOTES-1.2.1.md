# X99D3M4FanInit 1.2.1

Bug-fix release for installation on Windows PowerShell 5.1.

- Changed installer, status and uninstaller PowerShell output to encoding-safe ASCII;
- Replaced the extension filter with a Windows PowerShell 5.1-compatible form;
- No change to the validated Super I/O register operation or safety checks.

Users of v1.2.0 should download this version and run `Install.cmd` again. The v1.2.0 parser error happened before installation, so it did not modify the scheduled task or hardware register.
