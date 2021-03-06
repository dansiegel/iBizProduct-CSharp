﻿// Copyright (c) iBizVision - 2014
// Author: Dan Siegel

using System;
using System.Configuration;
using System.IO;
using System.Security.Principal;
using System.Text;
using iBizProduct.Security;
using iBizProduct.Ultilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iBizProduct
{
    /// <summary>
    /// This provides a base class for Settings Classes to inherit from. This provides a context to allow 
    /// Product settings to be stored in a common iBizProduct settings file, the Environment, or Configuration
    /// File. Settings Classes should override the Application Name. 
    /// </summary>
    public class SettingsBase
    {
        /// <summary>
        /// Application Name. 
        /// </summary>
        public static string ApplicationName = "iBizProduct v3";

        /// <summary>
        /// Settings File location. Default is in the UserProfile folder of the user executing the application.
        /// </summary>
        public static string SettingsFile = string.Format( @"{0}\{1}", Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), @"ProductSettings.ibpv3" );

        /// <summary>
        /// This contains the settings that iBizProduct has to work with. 
        /// </summary>
        public static ProductSettings Settings;

        //public static ProductEntity ProductContext;

        static SettingsBase()
        {
            Settings = new ProductSettings();
            ReadSettings();
        }

        /// <summary>
        /// Check to see if the code is currently executing in Elevated mode. 
        /// </summary>
        public static bool IsElevated
        {
            get
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal( identity );
                return principal.IsInRole( WindowsBuiltInRole.Administrator );
            }
        }

        /// <summary>
        /// Add a Setting to the Project Settings. This will automatically persist new settings to the Settings File.
        /// </summary>
        /// <param name="key">Settings Key</param>
        /// <param name="value">Settings Value</param>
        /// <param name="encryption">Encryption type used.</param>
        public static void AddSetting( string key, string value, EncryptionType encryption = EncryptionType.None )
        {
            if( Settings == null || Settings.Count == 0 )
                ReadSettings();

            var settingValue = new ProductSettingValue()
            {
                Encryption = encryption,
                Value = value
            };

            Settings.Add( key, settingValue );
            WriteSettings();
        }

        /// <summary>
        /// This will read in the Settings File and populate the Settings Object.
        /// </summary>
        public static void ReadSettings()
        {
            try
            {
                // We only want to read in the file assuming that it actually exists.
                if( File.Exists( SettingsFile ) )
                {
                    using( StreamReader r = new StreamReader( SettingsFile ) )
                    {
                        string json = r.ReadToEnd();
                        Settings = JsonConvert.DeserializeObject<ProductSettings>( json );
                    }
                }
            }
            catch( Exception ex )
            {
                // This is most likely due to a parsing error.
                iBizLog.WriteException( ex );
            }
        }

        /// <summary>
        /// This will persist the Settings to the Settings File.
        /// </summary>
        public static void WriteSettings()
        {
            try
            {
                var json = new JObject( Settings );

                File.WriteAllText( SettingsFile, json.ToString() );
            }
            catch( Exception ex )
            {
                // This is most likely due to the application running without proper file permissions
                //iBizLog.WriteException( ex );
                Console.WriteLine( "Unable to write the file." );
            }
        }

        /// <summary>
        /// Get the application setting. This also allows you to specify a default value. This will check 
        /// The Settings Value from the settings file located in the MyDocuments directory of the user. If
        /// no value can be found it will check the Environment, and finally the Configuration AppSettings.
        /// </summary>
        /// <typeparam name="T">Type of the parameter to return</typeparam>
        /// <param name="SettingName">The Name of the Variable</param>
        /// <param name="EncryptionType">Specify the Type of Encryption Used. DEFAULT: None</param>
        /// <returns>Value located in the Environment </returns>
        /// <exception cref="iBizException">Throws an iBizException due to a NullReference Exception if no value can be found.</exception>
        public static T GetSetting<T>( string SettingName, EncryptionType EncryptionType = EncryptionType.None )
        {
            string settingValue;
            try
            {
                var settingObject = Settings[ SettingName ];
                settingValue = settingObject.Value;
                if( String.IsNullOrEmpty( settingValue ) )
                    throw new Exception();

                EncryptionType = settingObject.Encryption;
            }
            catch( Exception )
            {
                settingValue = Environment.GetEnvironmentVariable( SettingName );

                if( String.IsNullOrEmpty( settingValue ) )
                {
                    settingValue = ConfigurationManager.AppSettings[ SettingName ];

                    if( String.IsNullOrEmpty( settingValue ) )
                    {
                        var NullException = new NullReferenceException( string.Format( "There is currently no definition that {0} can use for: {1}.", ApplicationName, SettingName ) );
                        throw new NullSettingValueException( SettingName, NullException );
                    }
                }
            }

            iBizSecure Encryptor = new iBizSecure();
            return Encryptor.Decrypt<T>( settingValue, EncryptionType );
        }

        /// <summary>
        /// Get the application setting. This also allows you to specify a default value. This will check<br/>
        /// The Settings Value from the settings file located in the MyDocuments directory of the user. If<br/>
        /// no value can be found it will check the Environment, and finally the Configuration AppSettings.
        /// Should no value be found it will return the default value specified.
        /// </summary>
        /// <typeparam name="T">Type of the parameter to return</typeparam>
        /// <param name="SettingName">The Name of the Variable</param>
        /// <param name="Default">Default Value</param>
        /// <param name="EncryptionType">Specify the Type of Encryption Used. DEFAULT: None</param>
        /// <returns>Value located in the Environment or Default Value if not found.</returns>
        public static T GetSetting<T>( string SettingName, T Default, EncryptionType EncryptionType = EncryptionType.None )
        {
            try
            {
                return GetSetting<T>( SettingName, EncryptionType );
            }
            catch( Exception )
            {
                return Default;
            }
        }

        //public static T GetRuntimeSetting<T>( string SettingName, EncryptionType EncryptionType = EncryptionType.None )
        //{
        //    if( ProductContext == null )
        //    {
        //        var e = new NullSettingValueException( "Product Context was not initialized" );
        //        throw new iBizException( "The current configuration does not allow for getting runtime settings." );
        //    }

        //    var config = ProductContext.RuntimeConfigs.FirstOrDefault( rc => rc.Key == SettingName );

        //    if( config != null )
        //        return DecryptSetting<T>( config.Value, EncryptionType.None );

        //    var NullException = new NullReferenceException( string.Format( "There is currently no definition that {0} can use for: {1}.", ApplicationName, SettingName ) );
        //    throw new iBizException( string.Format( "{0} does not exist please check your environmental settings.", SettingName ), NullException );
        //}

        //public static T GetRuntimeSetting<T>( string SettingName, T Default, EncryptionType EncryptionType = EncryptionType.None )
        //{
        //    try
        //    {
        //        return GetRuntimeSetting<T>( SettingName, EncryptionType );
        //    }
        //    catch
        //    {
        //        return Default;
        //    }
        //}

        public static T ConvertTo<T>( string Setting )
        {
            Type t = typeof( T );

            if( t.IsEnum )
            {
                return ( T )Enum.Parse( t, Setting, true );
            }

            return ( T )Convert.ChangeType( Setting, typeof( T ) );
        }
    }
}
