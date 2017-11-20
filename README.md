# LC-IMS-MS Feature Finder

The LC-IMS-MS Feature Finder finds LC-IMS-MS features and conformers 
using deisotoped features from DeconTools. Required files are a 
DeconTools _isos.csv file plus the corresponding 
.UIMF file if UseConformationDetection is True.

A features file is created by clustering together isotopic profiles that are
believed to be from the same isotopic signature eluting over a period of time.
Each feature in the file has representative mass, elution time, drift time and
charge state values that define the feature.

## Syntax

```
LCMSFeatureFinder.exe SettingsFile.ini
```

The settings file defines the input file path and the output directory.
It also defines a series of settings used to aid the Feature Finder.

To see an example settings file, use `LCMSFeatureFinder.exe /X` \
To see an example file for parameter DeconToolsFilterFileName, use `LCMSFeatureFinder.exe /Y`

## Contacts

Written by Kevin Crowell for the Department of Energy (PNNL, Richland, WA) \
Copyright 2017, Battelle Memorial Institute.  All Rights Reserved. \
E-mail: proteomics@pnnl.gov \
Website: https://panomics.pnl.gov/ or https://omics.pnl.gov

## License

Licensed under the Apache License, Version 2.0; you may not use this file except
in compliance with the License.  You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
