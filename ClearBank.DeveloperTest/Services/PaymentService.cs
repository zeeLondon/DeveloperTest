// ===============================
// Author: Zbigniew Hibner
// Created: 22.01.2021
// TODO: Provided code never set the MakePaymentResult Success to true, double check the requirement.
// - Add logging
// ===============================


using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Types;
using ClearBank.DeveloperTest.Validators;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService : IPaymentService
    {
        readonly DataStoreConfiguration _dataStoreConfiguration;
        readonly IAccountDataStore _accountDataStore;
        readonly IDictionary<PaymentScheme, IPaymentValidator> _validators = new Dictionary<PaymentScheme, IPaymentValidator>();

        const string BackupDataStoreConfigValue = "Backup";

        public PaymentService(IOptions<DataStoreConfiguration> dataStoreConfiguration, IAccountDataStore accountDataStore, IAccountDataStore backupAccountDataStore)
        {
            _dataStoreConfiguration = dataStoreConfiguration.Value;

            // initialize data store
            if (_dataStoreConfiguration.DataStoreType == BackupDataStoreConfigValue)
            {
                _accountDataStore = backupAccountDataStore;
            }
            else
            {
                _accountDataStore = accountDataStore;
            }

            AddPaymentValidators();
        }

        private void AddPaymentValidators()
        {
            _validators.Add(PaymentScheme.Bacs, new PaymentValidator());
            _validators.Add(PaymentScheme.Chaps, new ChapsValidator());
            _validators.Add(PaymentScheme.FasterPayments, new FasterPaymentsValidator());
        }

        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            Account account = _accountDataStore.GetAccount(request.DebtorAccountNumber);
          
            var result = new MakePaymentResult();

            var validator = _validators[request.PaymentScheme];

            if (validator != null)
            {
                result.Success = validator.IsValid(request, account);
            }

            if (result.Success)
            {
                account.Balance -= request.Amount;

                _accountDataStore.UpdateAccount(account);
            }

            return result;
        }
    }
}
