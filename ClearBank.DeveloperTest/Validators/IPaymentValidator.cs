using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Validators
{
    public interface IPaymentValidator
    {
        bool IsValid(MakePaymentRequest request, Account account);
    }
}
