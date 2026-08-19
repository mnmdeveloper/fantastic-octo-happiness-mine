# CutVPN component payload

Place the three official component installers in this folder before building the setup EXE.

Expected filenames:
- `Goose.exe`
- `CockroachOnDesktop.exe`
- `Workrave.exe`

The setup wizard treats these as visible, user-selected components. It does not hide them or masquerade them as Windows components.

After copying the selected payloads into `%LOCALAPPDATA%\CutVPN\payload`, a later CutVPN component step can launch each installer visibly.
