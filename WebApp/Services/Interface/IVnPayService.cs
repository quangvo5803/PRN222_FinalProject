using BusinessObject.Model;

namespace WebApp.Services.Interface
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel requestModel);
        VnPaymentResponseModel PaymentExecute(IQueryCollection query);
    }
}
