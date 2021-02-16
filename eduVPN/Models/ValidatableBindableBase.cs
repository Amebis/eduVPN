/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace eduVPN.Models
{
    /// <summary>
    /// Base class for view models
    /// </summary>
    public class ValidatableBindableBase : BindableBase, INotifyDataErrorInfo
    {
        #region Fields

        /// <summary>
        /// Error storage container
        /// </summary>
        private readonly ErrorsContainer<ValidationResult> ErrorsContainer;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a validatable bindable base class
        /// </summary>
        public ValidatableBindableBase()
        {
            ErrorsContainer = new ErrorsContainer<ValidationResult>(propertyName =>
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                    RaisePropertyChanged(nameof(HasErrors));
                });
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, propertyName);
            if (result && !string.IsNullOrEmpty(propertyName))
                ValidateProperty(propertyName);

            return result;
        }

        /// <inheritdoc/>
        protected override bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, onChanged, propertyName);
            if (result && !string.IsNullOrEmpty(propertyName))
                ValidateProperty(propertyName);

            return result;
        }

        /// <summary>
        /// Performs a validation of a property.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns><c>true</c> if the property is valid; <c>false</c> otherwise</returns>
        public bool ValidateProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var propertyInfo = GetType().GetRuntimeProperty(propertyName);
            if (propertyInfo == null)
                throw new ArgumentException(string.Format(Resources.Strings.ErrorInvalidPropertyName, propertyName), nameof(propertyName));

            var propertyErrors = new List<ValidationResult>();
            bool isValid = TryValidateProperty(propertyInfo, propertyErrors);
            ErrorsContainer.SetErrors(propertyInfo.Name, propertyErrors);

            return isValid;
        }

        /// <summary>
        /// Performs a validation of a property, adding the results in the <paramref name="propertyErrors"/> list.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of the property to validate</param>
        /// <param name="propertyErrors">A list containing the current error messages of the property.</param>
        /// <returns><c>true</c> if the property is valid; <c>false</c> otherwise</returns>
        private bool TryValidateProperty(PropertyInfo propertyInfo, List<ValidationResult> propertyErrors)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(this) { MemberName = propertyInfo.Name };
            var propertyValue = propertyInfo.GetValue(this);
            bool isValid = Validator.TryValidateProperty(propertyValue, context, results);
            if (results.Any())
                propertyErrors.AddRange(results);

            return isValid;
        }

        #endregion

        #region INotifyDataErrorInfo support
        /// <summary>
        /// Gets a value that indicates whether the entity has validation errors.
        /// </summary>
        public bool HasErrors { get => ErrorsContainer.HasErrors; }

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve validation errors for; or <c>null</c> or empty, to retrieve entity-level errors.</param>
        /// <returns>The validation errors for the property or entity.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsContainer.GetErrors(propertyName);
        }

        /// <summary>
        /// Occurs when the validation errors have changed for a property or for the entire entity.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }
}
