/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.ComponentModel;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
        private ErrorsContainer<ValidationResult> _errors_container;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a validatable bindable base class
        /// </summary>
        public ValidatableBindableBase()
        {
            _errors_container = new ErrorsContainer<ValidationResult>(property_name =>
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property_name));
                    RaisePropertyChanged(nameof(HasErrors));
                });
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, propertyName);
            if (result && !String.IsNullOrEmpty(propertyName))
                ValidateProperty(propertyName);

            return result;
        }

        /// <inheritdoc/>
        protected override bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, onChanged, propertyName);
            if (result && !String.IsNullOrEmpty(propertyName))
                ValidateProperty(propertyName);

            return result;
        }

        /// <summary>
        /// Performs a validation of a property.
        /// </summary>
        /// <param name="property_name">Property name</param>
        /// <returns><c>true</c> if the property is valid; <c>false</c> otherwise</returns>
        public bool ValidateProperty(string property_name)
        {
            if (String.IsNullOrEmpty(property_name))
                throw new ArgumentNullException(nameof(property_name));

            var property_info = GetType().GetRuntimeProperty(property_name);
            if (property_info == null)
                throw new ArgumentException(String.Format(Resources.Strings.ErrorInvalidPropertyName, property_name), nameof(property_name));

            var property_errors = new List<ValidationResult>();
            bool is_valid = TryValidateProperty(property_info, property_errors);
            _errors_container.SetErrors(property_info.Name, property_errors);

            return is_valid;
        }

        /// <summary>
        /// Performs a validation of a property, adding the results in the <paramref name="property_errors"/> list.
        /// </summary>
        /// <param name="property_info">The <see cref="PropertyInfo"/> of the property to validate</param>
        /// <param name="property_errors">A list containing the current error messages of the property.</param>
        /// <returns><c>true</c> if the property is valid; <c>false</c> otherwise</returns>
        private bool TryValidateProperty(PropertyInfo property_info, List<ValidationResult> property_errors)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(this) { MemberName = property_info.Name };
            var property_value = property_info.GetValue(this);
            bool is_valid = Validator.TryValidateProperty(property_value, context, results);
            if (results.Any())
                property_errors.AddRange(results);

            return is_valid;
        }

        #endregion

        #region INotifyDataErrorInfo support
        /// <summary>
        /// Gets a value that indicates whether the entity has validation errors.
        /// </summary>
        public bool HasErrors { get => _errors_container.HasErrors; }

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve validation errors for; or <c>null</c> or empty, to retrieve entity-level errors.</param>
        /// <returns>The validation errors for the property or entity.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            return _errors_container.GetErrors(propertyName);
        }

        /// <summary>
        /// Occurs when the validation errors have changed for a property or for the entire entity.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }
}
