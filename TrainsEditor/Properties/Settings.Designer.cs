﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TrainsEditor.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\..\\ZastavkyLinkyDopravci.xml")]
        public string StopsAndLinesFileName {
            get {
                return ((string)(this["StopsAndLinesFileName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\..\\SR70_2021.csv")]
        public string SR70Stops {
            get {
                return ((string)(this["SR70Stops"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\temp\\jrspoje\\GVD2023+KADR")]
        public string RepositoryFolder {
            get {
                return ((string)(this["RepositoryFolder"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\..\\sit_vlak.csv")]
        public string TrackNetworkFile {
            get {
                return ((string)(this["TrackNetworkFile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\temp\\jrspoje\\gtfs_vlaky")]
        public string OutputFolder {
            get {
                return ((string)(this["OutputFolder"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("c:\\temp\\jrspoje\\log")]
        public string LogFolder {
            get {
                return ((string)(this["LogFolder"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"					<ArrayOfTripDirectionSpec xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
						<TripDirectionSpec>
							<LineName>S33</LineName>
							<TrainNumber>8515</TrainNumber>
						</TripDirectionSpec>
                        <TripDirectionSpec>
							<LineName>S33</LineName>
							<TrainNumber>8518</TrainNumber>
						</TripDirectionSpec>
						<TripDirectionSpec>
							<LineName>S9</LineName>
							<TrainNumber>2540</TrainNumber>
						</TripDirectionSpec>
						<TripDirectionSpec>
							<LineName>S9</LineName>
							<TrainNumber>2509</TrainNumber>
						</TripDirectionSpec>
						<TripDirectionSpec>
							<LineName>S2</LineName>
							<TrainNumber>5802</TrainNumber>
						</TripDirectionSpec>
						<TripDirectionSpec>
							<LineName>S2</LineName>
							<TrainNumber>5805</TrainNumber>
						</TripDirectionSpec>
					</ArrayOfTripDirectionSpec>")]
        public global::System.Collections.Generic.List<TrainsEditor.GtfsExport.TripDirectionSpec> RepresentativeTrips {
            get {
                return ((global::System.Collections.Generic.List<TrainsEditor.GtfsExport.TripDirectionSpec>)(this["RepresentativeTrips"]));
            }
        }
    }
}
