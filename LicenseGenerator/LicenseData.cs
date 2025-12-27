using System;

namespace LicenseGenerator;

public class LicenseData
{
    public string MachineId { get; set; }
    public string Product { get; set; }
    public string Version { get; set; }
    public DateTime Expiry { get; set; }
    public string Signature { get; set; }

}
