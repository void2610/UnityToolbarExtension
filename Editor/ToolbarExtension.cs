using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace YujiAp.UnityToolbarExtension.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtension
    {
        private const string ToolbarZoneLeftAlignName = "ToolbarZoneLeftAlign";
        private const string ToolbarZoneRightAlignName = "ToolbarZoneRightAlign";
        private const string ToolbarOverlayTopName = "overlay-toolbar__top";
        private const string ToolbarOverlayBeforeSpacerClassName = "unity-overlay-container__before-spacer-container";
        private const string ToolbarOverlayMiddleClassName = "unity-overlay-container__middle-container";
        private const string ToolbarOverlayAfterSpacerClassName = "unity-overlay-container__after-spacer-container";
        private const string OverlayElementClassName = "unity-overlay";
        private const string OverlayHorizontalLayoutClassName = "overlay-layout--toolbar-horizontal";
        private const string OverlayExpandedClassName = "unity-overlay--expanded";
        private const string PlayModeOverlayName = "PlayMode";
        private const string ToolbarExtensionLeftContainerName = "ToolbarExtensionLeftContainer";
        private const string ToolbarExtensionRightContainerName = "ToolbarExtensionRightContainer";
        private const string ToolbarExtensionMiddleLeftContainerName = "ToolbarExtensionMiddleLeftContainer";
        private const string ToolbarExtensionMiddleRightContainerName = "ToolbarExtensionMiddleRightContainer";
        private const string ToolbarExtensionLeftAlignName = "LeftAlign";
        private const string ToolbarExtensionRightAlignName = "RightAlign";

        static ToolbarExtension()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var toolbar = GetToolbar();
            if (toolbar == null)
            {
                return;
            }

            if (!TryGetToolbarZones(toolbar, out var toolbarZoneLeftAlign, out var toolbarZoneRightAlign, out var useCompactContainer))
            {
                return;
            }

            // Retinaディスプレイから外部ディスプレイにウィンドウを移動した際などにリセットされてしまうため、
            // 描画済みかどうかを毎フレーム確認し、描画されていなかったら描画するようにしておく
            var leftContainer = toolbarZoneLeftAlign.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = toolbarZoneRightAlign.Q(ToolbarExtensionRightContainerName);
            if (leftContainer != null && rightContainer != null)
            {
                if (useCompactContainer)
                {
                    ApplyCompactLayout(leftContainer);
                    ApplyCompactLayout(rightContainer);
                    EnsureCompactOuterContainerOrder(toolbarZoneRightAlign, rightContainer, toolbarZoneRightAlign.childCount - 1);
                    EnsureCompactCenterContainers(toolbar, out var centerLeftContainer, out var centerRightContainer);
                    DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), centerLeftContainer,
                        centerRightContainer, rightContainer.Q(ToolbarExtensionRightAlignName));
                    return;
                }

                // 描画済みなので終了
                return;
            }

            if (leftContainer == null)
            {
                leftContainer = CreateContainerElement();
                leftContainer.name = ToolbarExtensionLeftContainerName;
                if (useCompactContainer)
                {
                    ApplyCompactLayout(leftContainer);
                }
                toolbarZoneLeftAlign.Insert(toolbarZoneLeftAlign.childCount, leftContainer);
            }

            if (rightContainer == null)
            {
                rightContainer = CreateContainerElement();
                rightContainer.name = ToolbarExtensionRightContainerName;
                if (useCompactContainer)
                {
                    ApplyCompactLayout(rightContainer);
                }
                toolbarZoneRightAlign.Insert(toolbarZoneRightAlign.childCount, rightContainer);
            }

            if (useCompactContainer)
            {
                EnsureCompactCenterContainers(toolbar, out var centerLeftContainer, out var centerRightContainer);
                DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), centerLeftContainer,
                    centerRightContainer, rightContainer.Q(ToolbarExtensionRightAlignName));
                return;
            }

            DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
        }

        private static VisualElement GetToolbar()
        {
            var toolbarType = Type.GetType("UnityEditor.Toolbar,UnityEditor");
            if (toolbarType == null)
            {
                return null;
            }

            var getField = toolbarType.GetField("get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var getValue = getField?.GetValue(null);
            if (getValue == null)
            {
                var instanceProperty = toolbarType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                getValue = instanceProperty?.GetValue(null);
            }
            if (getValue == null)
            {
                return null;
            }

            var windowBackendProperty = toolbarType.GetProperty("windowBackend", BindingFlags.Instance | BindingFlags.NonPublic);
            var windowBackendValue = windowBackendProperty?.GetValue(getValue);
            if (windowBackendValue == null)
            {
                return null;
            }

            var iWindowBackendType = Type.GetType("UnityEditor.IWindowBackend,UnityEditor");
            if (iWindowBackendType == null)
            {
                return null;
            }

            var visualTreeProperty = iWindowBackendType.GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public);
            var visualTreeValue = visualTreeProperty?.GetValue(windowBackendValue);

            return visualTreeValue as VisualElement;
        }

        private static bool TryGetToolbarZones(VisualElement toolbar, out VisualElement leftZone, out VisualElement rightZone, out bool useCompactContainer)
        {
            leftZone = toolbar.Q(ToolbarZoneLeftAlignName);
            rightZone = toolbar.Q(ToolbarZoneRightAlignName);
            if (leftZone != null && rightZone != null)
            {
                useCompactContainer = false;
                return true;
            }

            var overlayToolbar = toolbar.Q(ToolbarOverlayTopName);
            if (overlayToolbar == null)
            {
                useCompactContainer = false;
                return false;
            }

            leftZone = overlayToolbar.Q(className: ToolbarOverlayBeforeSpacerClassName);
            rightZone = overlayToolbar.Q(className: ToolbarOverlayAfterSpacerClassName);
            useCompactContainer = true;

            return leftZone != null && rightZone != null;
        }

        private static VisualElement CreateContainerElement()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.FlexStart;
            root.style.flexGrow = 1;

            var leftAlign = new VisualElement();
            leftAlign.name = ToolbarExtensionLeftAlignName;
            leftAlign.style.flexDirection = FlexDirection.Row;
            leftAlign.style.alignItems = Align.Center;
            leftAlign.style.justifyContent = Justify.FlexStart;
            leftAlign.style.flexGrow = 1;
            root.Add(leftAlign);

            var flexSpacer = new VisualElement();
            flexSpacer.style.flexGrow = 1;
            root.Add(flexSpacer);

            var rightAlign = new VisualElement();
            rightAlign.name = ToolbarExtensionRightAlignName;
            rightAlign.style.flexDirection = FlexDirection.Row;
            rightAlign.style.alignItems = Align.Center;
            rightAlign.style.justifyContent = Justify.FlexEnd;
            rightAlign.style.flexGrow = 1;
            root.Add(rightAlign);

            return root;
        }

        private static void ApplyCompactLayout(VisualElement container)
        {
            container.AddToClassList(OverlayElementClassName);
            container.AddToClassList(OverlayHorizontalLayoutClassName);
            container.AddToClassList(OverlayExpandedClassName);
            container.style.flexGrow = 0;

            var leftAlign = container.Q(ToolbarExtensionLeftAlignName);
            if (leftAlign != null)
            {
                leftAlign.style.flexGrow = 0;
            }

            var rightAlign = container.Q(ToolbarExtensionRightAlignName);
            if (rightAlign != null)
            {
                rightAlign.style.flexGrow = 0;
            }

            if (container.childCount > 1)
            {
                var spacer = container[1];
                spacer.style.display = DisplayStyle.None;
                spacer.style.flexGrow = 0;
            }
        }

        private static void EnsureCompactCenterContainers(VisualElement toolbar, out VisualElement centerLeftContainer, out VisualElement centerRightContainer)
        {
            centerLeftContainer = new VisualElement();
            centerRightContainer = new VisualElement();
            var overlayToolbar = toolbar.Q(ToolbarOverlayTopName);
            var middleZone = overlayToolbar?.Q(className: ToolbarOverlayMiddleClassName);
            var playModeOverlay = middleZone?.Q(PlayModeOverlayName);
            if (middleZone == null || playModeOverlay == null)
            {
                return;
            }

            centerLeftContainer = middleZone.Q(ToolbarExtensionMiddleLeftContainerName) ?? CreateCompactCenterContainer(ToolbarExtensionMiddleLeftContainerName);
            centerRightContainer = middleZone.Q(ToolbarExtensionMiddleRightContainerName) ?? CreateCompactCenterContainer(ToolbarExtensionMiddleRightContainerName);

            centerLeftContainer.RemoveFromHierarchy();
            centerRightContainer.RemoveFromHierarchy();

            var playModeIndex = middleZone.IndexOf(playModeOverlay);
            middleZone.Insert(Mathf.Clamp(playModeIndex, 0, middleZone.childCount), centerLeftContainer);

            playModeIndex = middleZone.IndexOf(playModeOverlay);
            middleZone.Insert(Mathf.Clamp(playModeIndex + 1, 0, middleZone.childCount), centerRightContainer);
        }

        private static VisualElement CreateCompactCenterContainer(string name)
        {
            var container = new VisualElement();
            container.name = name;
            container.AddToClassList(OverlayElementClassName);
            container.AddToClassList(OverlayHorizontalLayoutClassName);
            container.AddToClassList(OverlayExpandedClassName);
            container.style.flexGrow = 0;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.FlexStart;
            return container;
        }

        private static void EnsureCompactOuterContainerOrder(VisualElement parent, VisualElement container, int targetIndex)
        {
            if (parent == null || container == null)
            {
                return;
            }

            var currentIndex = parent.IndexOf(container);
            if (currentIndex == targetIndex || currentIndex < 0)
            {
                return;
            }

            container.RemoveFromHierarchy();
            parent.Insert(Mathf.Clamp(targetIndex, 0, parent.childCount), container);
        }

        private static void DrawElements(VisualElement leftSideLeftAlignRoot, VisualElement leftSideRightAlignRoot,
            VisualElement rightSideLeftAlignRoot, VisualElement rightSideRightAlignRoot)
        {
            // 既存の要素をクリア
            leftSideLeftAlignRoot.Clear();
            leftSideRightAlignRoot.Clear();
            rightSideLeftAlignRoot.Clear();
            rightSideRightAlignRoot.Clear();

            var settings = GetSettings();
            var toolbarElements = GetTypesImplementingInterface<IToolbarElement>();

            // 設定がある場合は設定に従って利用可能な型を更新
            settings?.UpdateElementSettings(toolbarElements.ToList());

            // LayoutType別に要素を配置
            var layoutTypes = (ToolbarElementLayoutType[]) Enum.GetValues(typeof(ToolbarElementLayoutType));

            foreach (var layoutType in layoutTypes)
            {
                var root = layoutType switch
                {
                    ToolbarElementLayoutType.LeftSideLeftAlign => leftSideLeftAlignRoot,
                    ToolbarElementLayoutType.LeftSideRightAlign => leftSideRightAlignRoot,
                    ToolbarElementLayoutType.RightSideLeftAlign => rightSideLeftAlignRoot,
                    ToolbarElementLayoutType.RightSideRightAlign => rightSideRightAlignRoot,
                    _ => throw new ArgumentOutOfRangeException()
                };

                // 設定からこのLayoutTypeの要素を順序付きで取得
                var orderedSettings = settings?.GetSettingsForLayoutType(layoutType) ?? new List<ToolbarElementSetting>();

                foreach (var elementSetting in orderedSettings)
                {
                    // 設定で無効化されている場合はスキップ
                    if (!elementSetting.IsEnabled)
                    {
                        continue;
                    }

                    var elementType = toolbarElements.FirstOrDefault(t => t.FullName == elementSetting.TypeName);
                    if (elementType == null)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(elementType) is IToolbarElement toolbarElement)
                    {
                        var element = toolbarElement.CreateElement();
                        if (element != null)
                        {
                            root.Add(element);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 特定のインターフェースを実装したすべての型を取得
        /// </summary>
        private static List<Type> GetTypesImplementingInterface<TInterface>()
        {
            var interfaceType = typeof(TInterface);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(GetAssemblyTypes)
                .Where(t => t != null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }

        private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// ToolbarExtensionSettingsを取得
        /// </summary>
        private static ToolbarExtensionSettings GetSettings()
        {
            return ToolbarExtensionSettings.Instance;
        }

        /// <summary>
        /// ツールバーを強制的に再描画
        /// </summary>
        public static void ForceRefresh()
        {
            var toolbar = GetToolbar();
            if (toolbar == null)
            {
                return;
            }

            if (!TryGetToolbarZones(toolbar, out var leftZone, out var rightZone, out _))
            {
                return;
            }

            var leftContainer = leftZone.Q(ToolbarExtensionLeftContainerName);
            var rightContainer = rightZone.Q(ToolbarExtensionRightContainerName);

            if (leftContainer != null && rightContainer != null)
            {
                DrawElements(leftContainer.Q(ToolbarExtensionLeftAlignName), leftContainer.Q(ToolbarExtensionRightAlignName),
                    rightContainer.Q(ToolbarExtensionLeftAlignName), rightContainer.Q(ToolbarExtensionRightAlignName));
            }
        }
    }
}
