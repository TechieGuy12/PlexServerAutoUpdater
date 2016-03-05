using System;
using System.Collections.Generic;
using System.Text;

namespace TE.LocalSystem.Msi
{
	/// <summary>
	/// Description of InstalledProduct.
	/// </summary>
	public class InstalledProduct
    {
		#region Public Variables
        public readonly string Guid;
		#endregion
		
        #region Properties
        public string InstalledProductName { get { return Api.getProperty(Guid, InstallProperty.InstalledProductName); } }
        public string VersionString { get { return Api.getProperty(Guid, InstallProperty.VersionString); } }
        public string HelpLink { get { return Api.getProperty(Guid, InstallProperty.HelpLink); } }
        public string HelpTelephone { get { return Api.getProperty(Guid, InstallProperty.HelpTelephone); } }
        public string InstallLocation { get { return Api.getProperty(Guid, InstallProperty.InstallLocation); } }
        public string InstallSource { get { return Api.getProperty(Guid, InstallProperty.InstallSource); } }
        public string InstallDate { get { return Api.getProperty(Guid, InstallProperty.InstallDate); } }
        public string Publisher { get { return Api.getProperty(Guid, InstallProperty.Publisher); } }
        public string LocalPackage { get { return Api.getProperty(Guid, InstallProperty.LocalPackage); } }
        public string URLInfoAbout { get { return Api.getProperty(Guid, InstallProperty.URLInfoAbout); } }
        public string URLUpdateInfo { get { return Api.getProperty(Guid, InstallProperty.URLUpdateInfo); } }
        public string VersionMinor { get { return Api.getProperty(Guid, InstallProperty.VersionMinor); } }
        public string VersionMajor { get { return Api.getProperty(Guid, InstallProperty.VersionMajor); } }
        public string ProductID { get { return Api.getProperty(Guid, InstallProperty.ProductID); } }
        public string RegCompany { get { return Api.getProperty(Guid, InstallProperty.RegCompany); } }
        public string RegOwner { get { return Api.getProperty(Guid, InstallProperty.RegOwner); } }
        public string Uninstallable { get { return Api.getProperty(Guid, InstallProperty.Uninstallable); } }
        public string State { get { return Api.getProperty(Guid, InstallProperty.State); } }
        public string PatchType { get { return Api.getProperty(Guid, InstallProperty.PatchType); } }
        public string LUAEnabled { get { return Api.getProperty(Guid, InstallProperty.LUAEnabled); } }
        public string DisplayName { get { return Api.getProperty(Guid, InstallProperty.DisplayName); } }
        public string MoreInfoURL { get { return Api.getProperty(Guid, InstallProperty.MoreInfoURL); } }
        public string LastUsedSource { get { return Api.getProperty(Guid, InstallProperty.LastUsedSource); } }
        public string LastUsedType { get { return Api.getProperty(Guid, InstallProperty.LastUsedType); } }
        public string MediaPackagePath { get { return Api.getProperty(Guid, InstallProperty.MediaPackagePath); } }
        public string DiskPrompt { get { return Api.getProperty(Guid, InstallProperty.DiskPrompt); } }
        #endregion
        
		#region Constructors
        public InstalledProduct(string guid)
        {
            this.Guid = guid;
        }
		#endregion
		
		#region Public Functions
        /// <summary>
        /// Enumerates all MSI installed products
        /// </summary>
        /// <returns>
        /// An enumeration containing InstalledProducts
        /// </returns>
        public static IEnumerable<InstalledProduct> Enumerate()
        {
            foreach (var guid in Api.EnumerateProducts())
                yield return new InstalledProduct(guid);
        }     

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var p in this.GetType().GetProperties())
            {
                try
                {
                    sb.AppendFormat("{0}:{1}\r\n", p.Name, p.GetValue(this));
                }
                catch
                { }
            }
            return sb.ToString();
        }
        #endregion
    }

}
