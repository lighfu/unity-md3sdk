using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public class MD3SelectionTests
    {
        [TestCase("NavBar")]
        [TestCase("NavRail")]
        [TestCase("NavDrawer")]
        [TestCase("TabBar")]
        public void ProgrammaticSelection_NotifiesOnceAfterSynchronizingItems(string kind)
        {
            var group = CreateGroup(kind);
            var notifications = new List<int>();
            group.Subscribe(index =>
            {
                notifications.Add(index);
                CollectionAssert.AreEqual(new[] { false, false, true }, group.ReadItems());
            });

            group.Select(2);

            CollectionAssert.AreEqual(new[] { 2 }, notifications);
            Assert.AreEqual(2, group.ReadIndex());
        }

        [TestCase("NavBar")]
        [TestCase("NavRail")]
        [TestCase("NavDrawer")]
        [TestCase("TabBar")]
        public void AssigningCurrentSelection_DoesNotNotify(string kind)
        {
            var group = CreateGroup(kind);
            int notifications = 0;
            group.Subscribe(_ => notifications++);

            group.Select(0);

            Assert.AreEqual(0, notifications);
            CollectionAssert.AreEqual(new[] { true, false, false }, group.ReadItems());
        }

        [TestCase("NavBar", -1)]
        [TestCase("NavRail", -1)]
        [TestCase("NavDrawer", -1)]
        [TestCase("TabBar", -1)]
        [TestCase("NavBar", 3)]
        [TestCase("NavRail", 3)]
        [TestCase("NavDrawer", 3)]
        [TestCase("TabBar", 3)]
        public void InvalidIndex_PreservesIndexAndClearsItemSelection(string kind, int index)
        {
            var group = CreateGroup(kind);
            var notifications = new List<int>();
            group.Subscribe(notifications.Add);

            group.Select(index);

            Assert.AreEqual(index, group.ReadIndex());
            CollectionAssert.AreEqual(new[] { false, false, false }, group.ReadItems());
            CollectionAssert.AreEqual(new[] { index }, notifications);
        }

        [TestCase("NavBar")]
        [TestCase("NavRail")]
        [TestCase("NavDrawer")]
        [TestCase("TabBar")]
        public void SelectingAnItem_NotifiesGroupOnceAndDeselectsPreviousItem(string kind)
        {
            var group = CreateGroup(kind);
            var notifications = new List<int>();
            group.Subscribe(notifications.Add);

            group.SelectItem(1);

            Assert.AreEqual(1, group.ReadIndex());
            CollectionAssert.AreEqual(new[] { false, true, false }, group.ReadItems());
            CollectionAssert.AreEqual(new[] { 1 }, notifications);
        }

        [TestCase("NavBar", 1, 2)]
        [TestCase("NavRail", 1, 2)]
        [TestCase("NavDrawer", 1, 2)]
        [TestCase("TabBar", 1, 2)]
        [TestCase("NavBar", 2, 1)]
        [TestCase("NavRail", 2, 1)]
        [TestCase("NavDrawer", 2, 1)]
        [TestCase("TabBar", 2, 1)]
        public void ChildNotificationSelectingAnotherIndex_PreservesLatestSelection(
            string kind, int requestedIndex, int nestedIndex)
        {
            var group = CreateGroup(kind);
            var notifications = new List<int>();
            var expectedItems = Enumerable.Range(0, 3).Select(index => index == nestedIndex).ToArray();
            group.Subscribe(index =>
            {
                notifications.Add(index);
                CollectionAssert.AreEqual(expectedItems, group.ReadItems());
            });
            group.SubscribeItem(0, selected =>
            {
                if (!selected) group.Select(nestedIndex);
            });

            group.Select(requestedIndex);

            Assert.AreEqual(nestedIndex, group.ReadIndex());
            CollectionAssert.AreEqual(expectedItems, group.ReadItems());
            CollectionAssert.AreEqual(new[] { nestedIndex }, notifications);
        }

        [TestCase("NavBar")]
        [TestCase("NavRail")]
        [TestCase("NavDrawer")]
        [TestCase("TabBar")]
        public void NestedSelectionReturningToRequestedIndex_DoesNotNotifyObsoleteRequest(string kind)
        {
            var group = CreateGroup(kind);
            var notifications = new List<int>();
            group.Subscribe(notifications.Add);
            bool redirected = false;
            group.SubscribeItem(1, selected =>
            {
                if (!selected || redirected) return;
                redirected = true;
                group.Select(2);
                group.Select(1);
            });

            group.Select(1);

            Assert.AreEqual(1, group.ReadIndex());
            CollectionAssert.AreEqual(new[] { false, true, false }, group.ReadItems());
            CollectionAssert.AreEqual(new[] { 2, 1 }, notifications);
        }

        [Test]
        public void DialogRadio_ClickNotifiesOnceAndRepeatedClickDoesNotNotify()
        {
            var radio = new MD3DialogRadio("Option");
            var notifications = new List<bool>();
            radio.changed += notifications.Add;

            using (var panel = new MD3TestPanel())
            {
                panel.Root.Add(radio);
                SendClick(radio);
                SendClick(radio);
            }

            Assert.IsTrue(radio.Selected);
            CollectionAssert.AreEqual(new[] { true }, notifications);
        }

        static void SendClick(VisualElement element)
        {
            using (var evt = ClickEvent.GetPooled())
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }

        static SelectionGroup CreateGroup(string kind)
        {
            // Detached elements avoid panel animation updates. Empty icon text avoids
            // depending on installed icon glyphs for these selection regressions.
            switch (kind)
            {
                case "NavBar":
                    var bar = new MD3NavBar(new[] { ("", "A"), ("", "B"), ("", "C") });
                    return CreateGroup<MD3NavBarItem>(bar, () => bar.SelectedIndex,
                        index => bar.SelectedIndex = index, callback => bar.changed += callback,
                        item => item.Selected, (item, selected) => item.Selected = selected,
                        (item, callback) => item.changed += callback);
                case "NavRail":
                    var rail = new MD3NavRail(new[] { ("", "A"), ("", "B"), ("", "C") });
                    return CreateGroup<MD3NavRailItem>(rail, () => rail.SelectedIndex,
                        index => rail.SelectedIndex = index, callback => rail.changed += callback,
                        item => item.Selected, (item, selected) => item.Selected = selected,
                        (item, callback) => item.changed += callback);
                case "NavDrawer":
                    var drawer = new MD3NavDrawer(new[] { ("", "A", 0), ("", "B", 0), ("", "C", 0) });
                    return CreateGroup<MD3NavDrawerItem>(drawer, () => drawer.SelectedIndex,
                        index => drawer.SelectedIndex = index, callback => drawer.changed += callback,
                        item => item.Selected, (item, selected) => item.Selected = selected,
                        (item, callback) => item.changed += callback);
                case "TabBar":
                    var tabs = new MD3TabBar(new[] { "A", "B", "C" });
                    return CreateGroup<MD3Tab>(tabs, () => tabs.SelectedIndex,
                        index => tabs.SelectedIndex = index, callback => tabs.changed += callback,
                        item => item.Selected, (item, selected) => item.Selected = selected,
                        (item, callback) => item.changed += callback);
                default:
                    throw new ArgumentException("Unknown selection group: " + kind, nameof(kind));
            }
        }

        static SelectionGroup CreateGroup<T>(VisualElement root, Func<int> readIndex,
            Action<int> select, Action<Action<int>> subscribe,
            Func<T, bool> readSelected, Action<T, bool> selectItem,
            Action<T, Action<bool>> subscribeItem) where T : VisualElement
        {
            var items = root.Children().OfType<T>().ToArray();
            return new SelectionGroup
            {
                ReadIndex = readIndex,
                Select = select,
                Subscribe = subscribe,
                ReadItems = () => items.Select(readSelected).ToArray(),
                SelectItem = index => selectItem(items[index], true),
                SubscribeItem = (index, callback) => subscribeItem(items[index], callback),
            };
        }

        sealed class SelectionGroup
        {
            internal Func<int> ReadIndex;
            internal Action<int> Select;
            internal Action<Action<int>> Subscribe;
            internal Func<bool[]> ReadItems;
            internal Action<int> SelectItem;
            internal Action<int, Action<bool>> SubscribeItem;
        }
    }
}
