using System;
using System.Collections.Generic;

namespace BusManagement.Model.Models;

public partial class TM_EMPLOYEE // TM_EMPLOYEE_AGENT
{
    public decimal ID { get; set; }

    public string? CHR_EMPLOYEE_ID { get; set; }

    public string? CHR_EMPLOYEE_NAME { get; set; }

    public string? CHR_EMPLOYEE_ADID { get; set; }

    public string? CHR_EMPLOYEE_MAIL { get; set; }

    public string? CHR_POSITION { get; set; }

    public string? CHR_POSITION_GROUP { get; set; }

    public string? CHR_COMPANY { get; set; }

    public string? CHR_DEPT_GROUP { get; set; }

    public string? CHR_DEPT { get; set; }

    public string? CHR_SECTION { get; set; }

    public string? CHR_COST_CENTER { get; set; }

    public string? CHR_GROUP { get; set; }

    public DateTime? DTM_JOIN_DATE { get; set; }

    public DateTime? DTM_LEAVE_DATE { get; set; }

    public DateTime? DTM_BIRTH_DATE { get; set; }

    public string? CHR_ID_NUMBER { get; set; }

    public DateTime? DTM_ISSUE_ID_DATE { get; set; }

    public string? CHR_ISSUE_ID_PLACE { get; set; }

    public string? CHR_GENDER { get; set; }

    public string? CHR_WORK_TYPE { get; set; }

    public string? CHR_TEMP_ADDRESS { get; set; }

    public string? CHR_PHONE_NO { get; set; }

    public string? CHR_PERMANENT_TEL_NO { get; set; }

    public string? CHR_PERMANENT_ADDRESS { get; set; }

    public string? CHR_PROVINCE { get; set; }

    public string? CHR_CHANEL_CV { get; set; }

    public string? CHR_STUDY_LEVEL { get; set; }

    public string? CHR_SCHOOL_NAME { get; set; }

    public string? CHR_STUDY_YEAR { get; set; }

    public string? CHR_MAJOR { get; set; }

    public string? CHR_WORKING_STATUS { get; set; }

    public string? CHR_ETHINIC { get; set; }

    public string? CHR_POSITION_CODE { get; set; }

    public string? CHR_POSITION_NAME { get; set; }

    public string? CHR_DEPT_CODE { get; set; }

    public string? CHR_DEPT_NAME { get; set; }

    public string? CHR_SEC_CODE { get; set; }

    public string? CHR_SEC_NAME { get; set; }

    public string? CHR_COST_CENTER_CODE { get; set; }

    public string? CHR_COST_CENTER_NAME { get; set; }

    public bool? BIT_OFICIAL_SEC { get; set; }

    public double? CHR_ANNUAL_LEAVE_DAY { get; set; }

    public double? CHR_TAKED_DAY { get; set; }

    public double? CHR_CAN_USE_DAY { get; set; }

    public string? CHR_CODE_GRADE { get; set; }

    public string? CHR_NOTE { get; set; }
}

public class TM_EMPLOYEE_WORKING // Working_ControlDB
{
    public int ID { get; set; }
    public string CHR_STAFF_ID { get; set; }
    public string NVCHR_NAME_FULL { get; set; }
    public string CHR_CODE_DEPT { get; set; }
    public string CHR_CODE_SEC { get; set; }
    public string CHR_CODE_GROUP { get; set; }
    public string CHR_CODE_CENTER { get; set; }
    public int? INT_WORKCENTER { get; set; }
    public int? ID_CLUSTER { get; set; }
    public int? ID_STAGE { get; set; }
    public string NVCHR_REMARK { get; set; }
    public int? ID_WORK_LEVEL { get; set; }
    public DateTime? DTM_TERMINATE_DATE { get; set; }
    public DateTime? DTM_JOIN_DATE { get; set; }
    public DateTime? DTM_JOIN_DATE_OPTION1 { get; set; }
    public string NVCHR_NUM_PHONE_STAFF { get; set; }
    public string NVCHR_STAGE_INCREASE { get; set; }
    public string CHR_CRT_USERID { get; set; }
    public DateTime? DTM_CREATE { get; set; }
    public string CHR_UPD_USERID { get; set; }
    public DateTime? DTM_UPDATE { get; set; }
    public int? INT_TOTAL_DAYOFF { get; set; }
    public string VCHR_SHIFT_CODE { get; set; }
    public string VCHR_NOTE1 { get; set; }
    public string VCHR_NOTE2 { get; set; }
    public string VCHR_OTHER1 { get; set; }
    public string VCHR_OTHER2 { get; set; }
    public string VCHR_OTHER3 { get; set; }
    public DateTime? DTM_JOIN_SECTION { get; set; }
    public DateTime? DTM_MATERNITY { get; set; }
    public string CHR_STATUS_DELETE { get; set; }
    public string VCHR_SKIP_CODE { get; set; }
}
