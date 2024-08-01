namespace Para.Bussiness.Notification;

public interface INotificationService
{
    public void SendEmail(string subject, string email, string content);
    void SendEmailToQueue(string subject, string email, string content);
}