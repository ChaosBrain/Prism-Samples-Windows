using AdventureWorks.UILogic.Models;
using AdventureWorks.UILogic.Services;
using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AdventureWorks.UILogic.ViewModels
{
    public class ShippingAddressPageViewModel : ViewModelBase
    {
        private readonly IShippingAddressUserControlViewModel _shippingAddressViewModel;
        private readonly IResourceLoader _resourceLoader;
        private readonly IAlertMessageService _alertMessageService;
        private readonly IAccountService _accountService;
        private readonly INavigationService _navigationService;
        private string _headerLabel;

        public ShippingAddressPageViewModel(IShippingAddressUserControlViewModel shippingAddressViewModel, IResourceLoader resourceLoader, IAlertMessageService alertMessageService, IAccountService accountService, INavigationService navigationService)
        {
            _shippingAddressViewModel = shippingAddressViewModel;
            _resourceLoader = resourceLoader;
            _alertMessageService = alertMessageService;
            _accountService = accountService;
            _navigationService = navigationService;

            SaveCommand = new DelegateCommand(async () => await SaveAsync());
        }

        public IShippingAddressUserControlViewModel ShippingAddressViewModel
        {
            get { return _shippingAddressViewModel; }
        }

        public string HeaderLabel
        {
            get { return _headerLabel; }
            private set { SetProperty(ref _headerLabel, value); }
        }

        public ICommand SaveCommand { get; private set; }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (await _accountService.VerifyUserAuthenticationAsync() == null)
            {
                return;
            }

            var addressId = e.Parameter as string;

            HeaderLabel = string.IsNullOrWhiteSpace(addressId)
                              ? _resourceLoader.GetString("AddShippingAddressTitle")
                              : _resourceLoader.GetString("EditShippingAddressTitle");

            ShippingAddressViewModel.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            ShippingAddressViewModel.OnNavigatingFrom(e, viewModelState, suspending);
        }

        private async Task SaveAsync()
        {
            if (ShippingAddressViewModel.ValidateForm())
            {
                string errorMessage = string.Empty;

                try
                {
                    await ShippingAddressViewModel.ProcessFormAsync();
                    _navigationService.GoBack();
                }
                catch (ModelValidationException mvex)
                {
                    DisplayValidationErrors(mvex.ValidationResult);
                }
                catch (Exception ex)
                {
                    errorMessage = string.Format(CultureInfo.CurrentCulture, _resourceLoader.GetString("GeneralServiceErrorMessage"), Environment.NewLine, ex.Message);
                }

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    await _alertMessageService.ShowAsync(errorMessage, _resourceLoader.GetString("ErrorServiceUnreachable"));
                }
            }
        }

        private void DisplayValidationErrors(ModelValidationResult modelValidationResults)
        {
            var errors = new Dictionary<string, ReadOnlyCollection<string>>();

            // Property keys format: address.{Propertyname}
            foreach (var propkey in modelValidationResults.ModelState.Keys)
            {
                string propertyName = propkey.Substring(propkey.IndexOf('.') + 1); // strip off order. prefix

                errors.Add(propertyName, new ReadOnlyCollection<string>(modelValidationResults.ModelState[propkey]));
            }

            if (errors.Count > 0)
            {
                ShippingAddressViewModel.Address.Errors.SetAllErrors(errors);
            }
        }
    }
}