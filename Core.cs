using MelonLoader;
using ModsPanel;
using SteamShelf;
using SteamShelf.Media;
using SteamShelf.Placeables;
using SteamShelf.PlayerTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(BoxroomQuickFill.Core), "BOXROOM Quick Fill", "1.0.0", "Rusty")]
[assembly: MelonGame("NestedLoop", "BOXROOM")]
[assembly: MelonAdditionalDependencies("ModsPanel")]

namespace BoxroomQuickFill
{
    public sealed class Core : MelonMod
    {
        private const string OwnerId = "Rusty.BoxroomQuickFill";
        private const int AllCompatibleMedia = int.MinValue;

        private static readonly FieldInfo LookingAtContainerField = typeof(PlayerInteractionTool)
            .GetField("currentLookingAtContainer", BindingFlags.Instance | BindingFlags.NonPublic);

        private static MelonPreferences_Entry<int> selectedMediaType;
        private static MelonPreferences_Entry<bool> enabled;

        public override void OnInitializeMelon()
        {
            MelonPreferences_Category preferences = MelonPreferences.CreateCategory("BoxroomQuickFill");
            selectedMediaType = preferences.CreateEntry("MediaType", AllCompatibleMedia, "What Quick Fill should place");
            enabled = preferences.CreateEntry("Enabled", true, "Enable the O shortcut");

            RegisterSettings();
            LoggerInstance.Msg("Ready. Look at a shelf and press O to fill empty slots A-Z.");
        }

        public override void OnUpdate()
        {
            if (enabled?.Value != true || Keyboard.current?.oKey.wasPressedThisFrame != true || ModsUi.IsMenuOpen)
                return;

            if (!Singleton<PlayerToolController>.HasInstance() ||
                !Singleton<PlayerToolController>.Instance.TryGetCurrentToolAsType(out PlayerInteractionTool interactionTool))
                return;

            if (interactionTool.IsHoldingProp)
            {
                ModsUi.ShowToast("Put down the item you are holding before using Quick Fill.", 4f);
                return;
            }

            PlaceableMediaContainer container = LookingAtContainerField?.GetValue(interactionTool) as PlaceableMediaContainer;
            if (container == null)
            {
                ModsUi.ShowToast("Look directly at the shelf you want to fill, then press O.", 4f);
                return;
            }

            Fill(container);
        }

        private void Fill(PlaceableMediaContainer container)
        {
            int wantedType = selectedMediaType?.Value ?? AllCompatibleMedia;
            List<IMediaItem> candidates;

            try
            {
                candidates = MediaLibraryRouter.GetKnownMedia()
                    .Where(item => IsAvailable(item, container, wantedType))
                    .OrderBy(item => item.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => (int)item.Ref.Type)
                    .ThenBy(item => item.Ref.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception exception)
            {
                LoggerInstance.Error($"Could not read the media libraries: {exception}");
                ModsUi.ShowToast("Quick Fill could not read the media libraries. Check Latest.log.", 6f);
                return;
            }

            int placed = 0;
            foreach (IMediaItem item in candidates)
                if (TryPlace(container, item))
                    placed++;

            string choice = GetSelectedChoiceLabel();
            if (placed > 0)
            {
                ModsUi.ShowToast($"Quick Fill added {placed} {(placed == 1 ? "item" : "items")} A-Z ({choice}).", 5f);
                LoggerInstance.Msg($"Filled {container.name}: placed {placed} of {candidates.Count} available items ({choice}).");
            }
            else if (candidates.Count == 0)
            {
                ModsUi.ShowToast($"No unspawned {choice.ToLowerInvariant()} are available for this shelf.", 5f);
            }
            else
            {
                ModsUi.ShowToast("This shelf has no empty space for the selected media.", 5f);
            }
        }

        private static bool IsAvailable(IMediaItem item, PlaceableMediaContainer container, int wantedType)
        {
            if (item == null || !item.IsFullyLoaded || item.IsSpawned || item.IsInHand)
                return false;
            if (PlaceableMediaContainer.IsMediaReserved(item.Ref))
                return false;
            if (wantedType != AllCompatibleMedia && (int)item.Ref.Type != wantedType)
                return false;
            return container.CanAccept(item);
        }

        private static bool TryPlace(PlaceableMediaContainer container, IMediaItem item)
        {
            MediaPlacement placement = container.ClampPlacement(MediaPlacement.Spine);
            for (int slot = 0; slot < container.MaxGameCount; slot++)
            {
                if (!container.CanPlaceAt(slot, item, placement))
                    continue;

                container.PlaceItem(item, slot, placement, playPlacedTween: false);
                return item.IsSpawned;
            }
            return false;
        }

        private static void RegisterSettings()
        {
            ModsPanelApi.RegisterSection(OwnerId, "BOXROOM Quick Fill", 115).Clear()
                .AddToggle(
                    "enabled",
                    "Enable quick shelf fill",
                    () => enabled?.Value == true,
                    value => Save(enabled, value))
                .AddDropdown(
                    "media-type",
                    "What Auto Fill (Media Type)",
                    () => GetMediaChoices().Select(choice => choice.Label).ToArray(),
                    GetSelectedChoiceIndex,
                    SetSelectedChoiceIndex)
                .AddLabel(
                    "instructions",
                    "Look directly at a shelf and press O. Empty spaces are filled A-Z using only media that is not already spawned or held anywhere in BOXROOM.");
        }

        private static int GetSelectedChoiceIndex()
        {
            List<MediaChoice> choices = GetMediaChoices();
            int wanted = selectedMediaType?.Value ?? AllCompatibleMedia;
            int index = choices.FindIndex(choice => choice.TypeId == wanted);
            return index >= 0 ? index : 0;
        }

        private static void SetSelectedChoiceIndex(int index)
        {
            List<MediaChoice> choices = GetMediaChoices();
            if (index >= 0 && index < choices.Count)
                Save(selectedMediaType, choices[index].TypeId);
        }

        private static string GetSelectedChoiceLabel()
        {
            List<MediaChoice> choices = GetMediaChoices();
            int wanted = selectedMediaType?.Value ?? AllCompatibleMedia;
            return choices.FirstOrDefault(choice => choice.TypeId == wanted)?.Label ?? choices[0].Label;
        }

        private static List<MediaChoice> GetMediaChoices()
        {
            var names = new Dictionary<int, string>
            {
                [0] = "Games",
                [1] = "Albums",
                [2] = "Movies / Videos",
                [1002] = "Books"
            };

            try
            {
                foreach (IMediaItem item in MediaLibraryRouter.GetKnownMedia())
                    if (item != null && !names.ContainsKey((int)item.Ref.Type))
                        names[(int)item.Ref.Type] = $"Media type {(int)item.Ref.Type}";
            }
            catch
            {
                // The libraries may not be configured while the title scene is loading.
            }

            AddMediaApiNames(names);

            var choices = new List<MediaChoice>
            {
                new MediaChoice(AllCompatibleMedia, "All shelf-compatible media")
            };
            choices.AddRange(names
                .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new MediaChoice(pair.Key, pair.Value)));
            return choices;
        }

        private static void AddMediaApiNames(IDictionary<int, string> names)
        {
            try
            {
                Assembly apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, "BR_MediaAPI", StringComparison.Ordinal));
                Type mediaApiType = apiAssembly?.GetType("BR_MediaAPI.MediaApi");
                IEnumerable registered = mediaApiType?.GetProperty("RegisteredTypes", BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null) as IEnumerable;
                if (registered == null)
                    return;

                foreach (object definition in registered)
                {
                    Type type = definition.GetType();
                    object rawId = type.GetProperty("Id")?.GetValue(definition);
                    string displayName = type.GetProperty("DisplayName")?.GetValue(definition) as string;
                    if (rawId is int id && !string.IsNullOrWhiteSpace(displayName))
                        names[id] = displayName;
                }
            }
            catch
            {
                // BR-MediaAPI is optional. Native types remain available without it.
            }
        }

        private static void Save<T>(MelonPreferences_Entry<T> entry, T value)
        {
            if (entry == null)
                return;
            entry.Value = value;
            MelonPreferences.Save();
        }

        private sealed class MediaChoice
        {
            internal MediaChoice(int typeId, string label)
            {
                TypeId = typeId;
                Label = label;
            }

            internal int TypeId { get; }
            internal string Label { get; }
        }
    }
}
