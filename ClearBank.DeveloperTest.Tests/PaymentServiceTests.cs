using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ClearBank.DeveloperTest.Tests
{
    public class PaymentServiceTests
    {
        const string DebtorAccountNumer = "DebtorAccountNumer";
        const string BackupDataStoreConfig = "Backup";

        private IOptions<DataStoreConfiguration> _dataStoreConfig;
        private Mock<IAccountDataStore> _accountDataMock;
        private Mock<IAccountDataStore> _backupAccountDataMock;

        [Theory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        [InlineData(PaymentScheme.FasterPayments)]
        public void Payments_WithoutExistingAccount_ShouldFail(PaymentScheme paymentScheme)
        {
            // Arrange
            InitDataStoreConfig();
            InitAccountDataStore(null);

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = paymentScheme,
                DebtorAccountNumber = DebtorAccountNumer
            };

            PaymentService sut = GetSystemUnderTest();

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        [InlineData(PaymentScheme.FasterPayments)]
        public void Payments_WithoutAllowedScheme_ShouldFail(PaymentScheme paymentScheme)
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account 
            { 
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments 
            };

            // exclude paymentscheme of the current payment
            var allowedPaymentScheme = (AllowedPaymentSchemes)(1 << (int)paymentScheme);
            account.AllowedPaymentSchemes = account.AllowedPaymentSchemes & ~allowedPaymentScheme;

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = paymentScheme,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void BacsPayments_WithAllowedScheme_ShouldSucceed()
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account { AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.Bacs,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void FasterPayments_WithoutEnoughFunds_ShouldFail()
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account { 
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments,
                Balance = 0
            };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.FasterPayments,
                Amount = 100,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.False(result.Success);
        }


        [Fact]
        public void FasterPayments_WithEnoughFunds_ShouldSucceed()
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account
            {
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments,
                Balance = 100
            };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.FasterPayments,
                Amount = 100,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(AccountStatus.Disabled)]
        [InlineData(AccountStatus.InboundPaymentsOnly)]
        public void ChapsPayments_WithOutLiveAccount_ShouldFail(AccountStatus accountStatus)
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account
            {
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments,
                Balance = 100,
                Status = accountStatus
            };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.Chaps,
                Amount = 100,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void ChapsPayments_WithLiveAccount_ShouldSucceed()
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account
            {
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments,
                Balance = 100,
                Status = AccountStatus.Live
            };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.Chaps,
                Amount = 100,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            var result = sut.MakePayment(makePaymentRequest);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void DataStore_WithBackupConfigured_ShouldUseBackupData()
        {
            // Arrange
            InitDataStoreConfig(BackupDataStoreConfig);

            var account = new Account
            {
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps
            };

            InitAccountDataStore(account);
            InitBackupAccountDataStore(account);


            var sut = GetSystemUnderTest();

            var request = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.Bacs,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            sut.MakePayment(request);

            // Assert
            _backupAccountDataMock.Verify(b => b.GetAccount(It.Is<string>(s => s == DebtorAccountNumer)));
            _backupAccountDataMock.Verify(b => b.UpdateAccount(It.IsAny<Account>()));

            _accountDataMock.Verify(b => b.GetAccount(It.Is<string>(s => s == DebtorAccountNumer)), Times.Never);
            _accountDataMock.Verify(b => b.UpdateAccount(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public void DataStore_WithOutBackupConfigured_ShouldUseSourceData()
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account
            {
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps
            };

            InitAccountDataStore(account);
            InitBackupAccountDataStore(account);


            var sut = GetSystemUnderTest();

            var request = new MakePaymentRequest
            {
                PaymentScheme = PaymentScheme.Bacs,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            sut.MakePayment(request);

            // Asses
            _accountDataMock.Verify(b => b.GetAccount(It.Is<string>(s => s == DebtorAccountNumer)));
            _accountDataMock.Verify(b => b.UpdateAccount(It.IsAny<Account>()));

            _backupAccountDataMock.Verify(b => b.GetAccount(It.Is<string>(s => s == DebtorAccountNumer)), Times.Never);
            _backupAccountDataMock.Verify(b => b.UpdateAccount(It.IsAny<Account>()), Times.Never);
        }

        [Theory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        [InlineData(PaymentScheme.FasterPayments)]
        public void Payments_ThatFailedValidation_ShouldNotUpdateBalance(PaymentScheme paymentScheme)
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account();

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = paymentScheme,
                DebtorAccountNumber = DebtorAccountNumer
            };

            // Act
            sut.MakePayment(makePaymentRequest);

            // Assert
            _accountDataMock.Verify(a => a.UpdateAccount(It.IsAny<Account>()), Times.Never);
        }


        [Theory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        [InlineData(PaymentScheme.FasterPayments)]
        public void Payments_ThatPassedValidation_ShouldUpdateBalance(PaymentScheme paymentScheme)
        {
            // Arrange
            InitDataStoreConfig();

            var account = new Account
            {
                Balance = 150,
                Status = AccountStatus.Live,
                AccountNumber = DebtorAccountNumer,
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps | AllowedPaymentSchemes.FasterPayments
            };

            InitAccountDataStore(account);

            var sut = GetSystemUnderTest();

            var makePaymentRequest = new MakePaymentRequest
            {
                PaymentScheme = paymentScheme,
                DebtorAccountNumber = DebtorAccountNumer,
                Amount = 100
            };

            // Act
            sut.MakePayment(makePaymentRequest);

            // Assert
            _accountDataMock.Verify(a => a.UpdateAccount(It.Is<Account>(a => a.AccountNumber == DebtorAccountNumer && a.Balance == 50 )));
        }

        private PaymentService GetSystemUnderTest()
        {
            return new PaymentService(_dataStoreConfig, _accountDataMock.Object, _backupAccountDataMock != null ? _backupAccountDataMock.Object : null);
        }

        private void InitAccountDataStore(Account account)
        {
            _accountDataMock = new Mock<IAccountDataStore>();
            _accountDataMock.Setup(m => m.GetAccount(It.Is<string>(d => d == DebtorAccountNumer)))
                .Returns(account);
        }

        private void InitBackupAccountDataStore(Account account)
        {
            _backupAccountDataMock = new Mock<IAccountDataStore>();
            _backupAccountDataMock.Setup(m => m.GetAccount(It.Is<string>(d => d == DebtorAccountNumer)))
                .Returns(account);
        }

        private void InitDataStoreConfig(string dataStoreType = null)
        {
            _dataStoreConfig = Options.Create(new DataStoreConfiguration { DataStoreType = dataStoreType });
        }
    }
}
