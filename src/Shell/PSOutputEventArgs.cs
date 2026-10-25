namespace PSLauncher.Shell;

public class PSOutputEventArgs(PSDataRecord record) : EventArgs
{
    public PSDataRecord Record => record;
}
