# CutVPN Agent contract

The installer creates `%LOCALAPPDATA%\CutVPN\agent.json` with a localhost-only control contract.

Default endpoint: `http://127.0.0.1:8765/command`

Allowed commands in the starter contract:
- `status`
- `visuals_on`
- `visuals_off`
- `random_error`
- `stop`

A future agent implementation should keep the listener on localhost by default, require an application secret, validate commands against this allowlist, and expose no arbitrary command/shell execution.
