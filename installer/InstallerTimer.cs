using FormsTimer = System.Windows.Forms.Timer;

namespace CutVPN.Setup;

// Resolves the ambiguous Timer reference in the installer while preserving the existing source.
internal sealed class Timer : FormsTimer
{
}
