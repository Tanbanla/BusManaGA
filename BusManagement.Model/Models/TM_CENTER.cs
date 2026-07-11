using System;
using System.Collections.Generic;

namespace BusManagement.Model.Models;

public partial class TM_CENTER
{
    public string CHR_CODE_CENTER { get; set; } = null!;

    public string CHR_CODE_DEPT { get; set; } = null!;

    public string CHR_CODE_SEC { get; set; } = null!;

    public string? NVCHR_CENTER { get; set; }

    public string? NVCHR_CENTER_SHORT { get; set; }

    public string? CHR_CRT_USERID { get; set; }

    public DateTime? DTM_CREATE { get; set; }

    public string? CHR_UPD_USERID { get; set; }

    public DateTime? DTM_UPDATE { get; set; }
}
