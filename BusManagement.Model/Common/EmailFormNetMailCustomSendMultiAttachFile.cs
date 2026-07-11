namespace BusManagement.Model.Common
{
    public class EmailFormNetMailCustomSendMultiAttachFile
    {
        public string title { get; set; }
        public string mail_from { get; set; }
        public string mail_to { get; set; }
        public string mail_cc { get; set; }
        public string mail_bcc { get; set; }
        public List<string> attachmentPaths { get; set; }
        public string body { get; set; }

        // danh sách ảnh nhúng cid trong body html
        public List<EmbeddedImage> embeddedImages { get; set; }
    }
    public class EmbeddedImage
    {
        public string ContentId { get; set; }   // dùng trong html: src='cid:{ContentId}'
        public string FilePath { get; set; }    // đường dẫn file local
    }
}
