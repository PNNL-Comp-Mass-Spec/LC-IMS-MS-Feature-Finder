The LC-IMS-MS Feature Finder finds LC-IMS-MS features and conformers 
using deisotoped features from DeconTools. Required files are a 
DeconTools _isos.csv file plus the corresponding 
.UIMF file if UseConformationDetection is True.

A features file is created by clustering together isotopic profiles that are
believed to be from the same isotopic signature eluting over a period of time.
Each feature in the file has representative mass, elution time, drift time and
charge state values that define the feature.

== Syntax ==

LCMSFeatureFinder.exe SettingsFile.ini

The settings file defines the input file path and the output directory.
It also defines a series of settings used to aid the Feature Finder.

To see an example settings file, use LCMSFeatureFinder.exe /X
To see an example file for parameter DeconToolsFilterFileName, use LCMSFeatureFinder.exe /Y

-------------------------------------------------------------------------------------------
Program written by Kevin Crowell for the Department of Energy (PNNL, Richland, WA) in 2010

E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/
-------------------------------------------------------------------------------------------
