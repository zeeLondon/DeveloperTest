using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Validators
{
    public class FasterPaymentsValidator : PaymentValidator
    {
        public override bool IsValid(MakePaymentRequest request, Account account)
        {
            var isValid = base.IsValid(request, account);

            if(isValid)
            {
                isValid = account.Balance >= request.Amount;
            }

            return isValid;
        }
    }
}
