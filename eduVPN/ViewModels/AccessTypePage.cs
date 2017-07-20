/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Access type selection wizard page
    /// </summary>
    public class AccessTypePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Set access type
        /// </summary>
        public ICommand SetAccessType
        {
            get
            {
                if (_set_access_type == null)
                {
                    _set_access_type = new DelegateCommand<AccessType?>(
                        // execute
                        param =>
                        {
                            Parent.AccessType = param.Value;
                            switch (param)
                            {
                                case AccessType.InstituteAccess: Parent.CurrentPage = Parent.InstanceSelectPage; break;
                            }
                        },

                        // canExecute
                        param =>
                        {
                            if (!param.HasValue) return false;
                            switch (param.Value)
                            {
                                case AccessType.InstituteAccess: return true;
                                default: return false;
                            }
                        });
                }
                return _set_access_type;
            }
        }
        private ICommand _set_access_type;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an access type selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public AccessTypePage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}
