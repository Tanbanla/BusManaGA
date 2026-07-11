using System;
using System.Collections.Generic;

namespace BusManagement.Model.Models;

public partial class TM_SECTION
{
    public int ID { get; set; }

    public string? CHR_CODE { get; set; }

    public string? CHR_NAME { get; set; }

    public bool? BIT_USED { get; set; }

    public int? INT_ORDER { get; set; }

    public string? CHR_REMARK { get; set; }

    public string? CHR_EMAIL_ADDRESS_SEC { get; set; }

    public string? CHR_SECTION_NAME { get; set; }
}
