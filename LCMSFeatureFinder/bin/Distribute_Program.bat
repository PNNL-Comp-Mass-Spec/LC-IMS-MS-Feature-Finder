xcopy Debug\*.exe                   C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy Debug\*.dll                   C:\DMS_Programs\LCMSFeatureFinder /D /Y /S
xcopy Debug\FeatureFinder.pdb       C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy Debug\LCMSFeatureFinder.pdb   C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy ..\..\Readme.md                  C:\DMS_Programs\LCMSFeatureFinder /D /Y

xcopy Debug\*.exe                   \\pnl\projects\omicssw\dms_programs\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy Debug\*.dll                   \\pnl\projects\omicssw\dms_programs\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y /S
xcopy Debug\FeatureFinder.pdb       \\pnl\projects\omicssw\dms_programs\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy Debug\LCMSFeatureFinder.pdb   \\pnl\projects\omicssw\dms_programs\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy ..\..\Readme.md                  \\pnl\projects\omicssw\dms_programs\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y

pause
