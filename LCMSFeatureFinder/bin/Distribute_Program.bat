xcopy Debug\*.exe                   C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy Debug\*.dll                   C:\DMS_Programs\LCMSFeatureFinder /D /Y /S
xcopy Debug\FeatureFinder.pdb       C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy Debug\LCMSFeatureFinder.pdb   C:\DMS_Programs\LCMSFeatureFinder /D /Y
xcopy ..\..\Readme.md                  C:\DMS_Programs\LCMSFeatureFinder /D /Y

xcopy Debug\*.exe                   \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy Debug\*.dll                   \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y /S
xcopy Debug\FeatureFinder.pdb       \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy Debug\LCMSFeatureFinder.pdb   \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y
xcopy ..\..\Readme.md                  \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\LCMSFeatureFinder /D /Y

pause
