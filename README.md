# Odin Tools Framework
Framework toolkit based on the Odin plugin implementation framework.

⭐️`LightWeightUI`</br>
Based on the lightweight UI interface framework of UGUI, Oidn visualization is adopted and YooAsset is used to achieve asynchronous loading of resource prefabs.

⭐️`ConfigHelper`</br>
Quickly creating a game configuration singleton will generate entries in the configuration window of the editor. The classification of the entries can be specified through the CategoryAttribute.

⭐️`LocalizationHelper`</br>
Based on the TextMeshPro localization tool, it is convenient to adapt to the localization languages of any country. During runtime, any country's language can be dynamically switched. It features extended functions such as Odin visual configuration, Luban configuration table, and font removal.

⭐️`AssetBundleHelper`</br>
Odin visualizes AssetBundle, which can be downloaded on demand based on requirements. The game asset package is not the same as AssetBundle. It is a resource package that is loaded at once. For example, if it needs to be hosted on a CDN, it might be split into multiple AB packages, or combined with shared AB packages. Supports custom collectors, AB naming customizations, filters, etc.
