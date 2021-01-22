using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Validators
{
    public class PaymentValidator : IPaymentValidator
    {
        public virtual bool IsValid(MakePaymentRequest request, Account account)
        {
            // translate paymentscheme to allowedpaymentschemes
            var allowedPaymentScheme = (AllowedPaymentSchemes)(1 << (int)request.PaymentScheme);

            if (account == null)
            {
                return false;
            }
            else if (!account.AllowedPaymentSchemes.HasFlag(allowedPaymentScheme))
            {
                return false;
            }

            return true;
        }
    }
}
