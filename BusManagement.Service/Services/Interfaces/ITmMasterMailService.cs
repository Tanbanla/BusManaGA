using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmMasterMailService : IBaseService<TM_MASTER_MAIL, int, TM_MASTER_MAILDTO>
{
    bool sendEmailNotify(string title, string mail_from, string mail_to, string mail_cc, string mail_bcc, string body, int priority);
    bool sendMailWithAttach(string title, string mail_from, string mail_to, string mail_cc, string mail_bcc, string body, string attachFile, int priority);
    Task<GenericResponse<bool>> SendEmailNotifyCustomSendMultiAttachFile(EmailFormNetMailCustomSendMultiAttachFile emailForm);
}
