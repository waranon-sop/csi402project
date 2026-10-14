using System;
using System.Collections.Generic;

namespace waranon_csi402.Models;

public partial class Labstudent
{
    public string StdId { get; set; } = null!;

    public string StdPassword { get; set; } = null!;

    public string StdName { get; set; } = null!;

    public string StdLastname { get; set; } = null!;
}
