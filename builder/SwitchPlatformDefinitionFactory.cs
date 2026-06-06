using helengine.baseplatform.Definitions;
using helengine.baseplatform.Profiles;

namespace helengine.switch_platform.builder;

/// <summary>
/// Creates the typed Switch platform metadata consumed by the editor.
/// </summary>
public static class SwitchPlatformDefinitionFactory {
    /// <summary>
    /// Creates the Switch platform definition.
    /// </summary>
    /// <returns>The Switch platform definition.</returns>
    public static PlatformDefinition Create() {
        return new PlatformDefinition(
            "switch",
            "Nintendo Switch",
            [
                new PlatformBuildProfileDefinition(
                    "debug",
                    "Debug",
                    "Debug Nintendo Switch homebrew build",
                    "switch-native",
                    "default",
                    [
                        new PlatformSettingDefinition(
                            "startup-clear-color",
                            "Startup Clear Color",
                            PlatformSettingKind.Text,
                            "#FFFF00",
                            true,
                            [])
                    ])
            ],
            [
                new PlatformGraphicsProfileDefinition(
                    "switch-native",
                    "Switch Native",
                    "Native Switch framebuffer-backed runtime profile.",
                    [
                        new PlatformSettingDefinition(
                            "default-width",
                            "Default Width",
                            PlatformSettingKind.Text,
                            "1280",
                            true,
                            []),
                        new PlatformSettingDefinition(
                            "default-height",
                            "Default Height",
                            PlatformSettingKind.Text,
                            "720",
                            true,
                            []),
                        new PlatformSettingDefinition(
                            "vsync-enabled",
                            "VSync Enabled",
                            PlatformSettingKind.Boolean,
                            "true",
                            true,
                            [])
                    ])
            ],
            [
                new PlatformAssetRequirementDefinition(
                    "scene",
                    "Scene",
                    false,
                    ["helen"])
            ],
            [
                new PlatformMaterialSchemaDefinition(
                    SwitchMaterialSchemaIds.StandardTexturedSchemaId,
                    "Switch Standard Textured",
                    ["switch-native"],
                    [
                        new PlatformMaterialFieldDefinition(
                            SwitchMaterialSchemaIds.TextureRelativePathFieldId,
                            "Texture",
                            PlatformMaterialFieldKind.Text,
                            string.Empty,
                            false,
                            []),
                        new PlatformMaterialFieldDefinition(
                            SwitchMaterialSchemaIds.DoubleSidedFieldId,
                            "Double Sided",
                            PlatformMaterialFieldKind.Boolean,
                            "false",
                            true,
                            []),
                        new PlatformMaterialFieldDefinition(
                            SwitchMaterialSchemaIds.VertexColorModeFieldId,
                            "Vertex Color Mode",
                            PlatformMaterialFieldKind.Choice,
                            "disabled",
                            true,
                            ["disabled", "multiply"]),
                        new PlatformMaterialFieldDefinition(
                            SwitchMaterialSchemaIds.BaseColorFieldId,
                            "Base Color",
                            PlatformMaterialFieldKind.Text,
                            "#FFFFFF",
                            true,
                            []),
                        new PlatformMaterialFieldDefinition(
                            SwitchMaterialSchemaIds.LightingModeFieldId,
                            "Lighting Mode",
                            PlatformMaterialFieldKind.Choice,
                            "lit",
                            true,
                            ["lit", "unlit"])
                    ])
            ],
            [
                new PlatformComponentSupportRule(
                    "helengine.meshcomponent",
                    PlatformComponentSupportKind.PassThrough,
                    "Switch packaged builds keep authored mesh components intact for generated-core runtime loading.",
                    string.Empty),
                new PlatformComponentSupportRule(
                    "helengine.spritecomponent",
                    PlatformComponentSupportKind.PassThrough,
                    "Switch packaged builds keep authored sprite components intact for generated-core runtime loading.",
                    string.Empty)
            ],
            [
                new PlatformCodegenProfileDefinition(
                    "default",
                    "Default",
                    "Switch C# to C++ codegen profile",
                    PlatformCodegenLanguage.Cpp,
                    PlatformSerializationEndianness.LittleEndian,
                    [
                        new PlatformSettingDefinition(
                            "write-conversion-report",
                            "Write Conversion Report",
                            PlatformSettingKind.Boolean,
                            "true",
                            true,
                            []),
                        new PlatformSettingDefinition(
                            "include-project-defined-preprocessor-symbols",
                            "Include Project Symbols",
                            PlatformSettingKind.Boolean,
                            "false",
                            true,
                            []),
                        new PlatformSettingDefinition(
                            "load-native-runtime-metadata",
                            "Load Native Runtime Metadata",
                            PlatformSettingKind.Boolean,
                            "true",
                            true,
                            [])
                    ])
            ],
            [
                new PlatformStorageProfileDefinition(
                    "romfs-package",
                    "RomFS Package",
                    PlatformStorageProfileKind.LooseFiles,
                    "switch-romfs-package",
                    allowContainerSegmentation: false)
            ],
            [
                new PlatformMediaProfileDefinition(
                    "switch-homebrew-nro",
                    "Switch Homebrew NRO",
                    PlatformMediaLayoutKind.InstallTree,
                    allowPhysicalDuplication: false,
                    preferLocalityOverDeduplication: true)
            ],
            new RuntimeGenerationContract(
                RuntimeMaterialResolutionMode.CookedPlatformOwned,
                true,
                PackagedPathPolicy.ContentRelativeOnly),
            PlatformHostDebugCapability.CreateDefault());
    }
}
