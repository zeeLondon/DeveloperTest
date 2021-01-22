using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Validators
{
    public class ChapsValidator : PaymentValidator
    {
        public override bool IsValid(MakePaymentRequest request, Account account)
        {
            var isValid = base.IsValid(request, account);

            if(isValid)
            {
                isValid = account.Status == AccountStatus.Live;
            }

            return isValid;
        }
    }
}
