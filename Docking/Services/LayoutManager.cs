using System;
using System.Linq;
using Avalonia.Layout;
using STACK_HUB.Docking.Models;

namespace STACK_HUB.Docking.Services;

public class LayoutManager
{
    /// <summary>
    /// Moves a source PaneNode to a specified target node and position.
    /// Returns the updated Root node (in case the root itself changed).
    /// </summary>
    public LayoutNode Dock(PaneNode sourcePane, LayoutNode targetNode, DockPosition position, ref LayoutNode root)
    {
        if (sourcePane == null || targetNode == null) throw new ArgumentNullException();

        // 1. Remove the source pane from its current location in the tree
        RemovePane(sourcePane, ref root);

        // 2. Perform docking relative to the target
        if (position == DockPosition.Center)
        {
            if (targetNode is TabGroupNode targetTabGroup)
            {
                targetTabGroup.Panes.Add(sourcePane);
                sourcePane.Parent = targetTabGroup;
                targetTabGroup.ActivePane = sourcePane;
            }
            else
            {
                throw new InvalidOperationException("Cannot dock to 'Center' of a non-TabGroup node.");
            }
        }
        else
        {
            // We are splitting around the target node
            SplitAndInsert(sourcePane, targetNode, position, ref root);
        }

        // 3. Clean up empty/redundant nodes created during removal or splitting
        root = NormalizeTree(root)!;
        return root;
    }

    /// <summary>
    /// Removes a pane from the tree.
    /// </summary>
    public void RemovePane(PaneNode pane, ref LayoutNode root)
    {
        if (pane.Parent is TabGroupNode tabGroup)
        {
            tabGroup.Panes.Remove(pane);
            pane.Parent = null;

            if (tabGroup.ActivePane == pane)
            {
                tabGroup.ActivePane = tabGroup.Panes.FirstOrDefault();
            }

            // Normalize tree to collapse empty tab group if needed
            root = NormalizeTree(root)!;
        }
    }

    /// <summary>
    /// Splits the space occupied by targetNode and inserts sourcePane into a new TabGroup.
    /// </summary>
    private void SplitAndInsert(PaneNode sourcePane, LayoutNode targetNode, DockPosition position, ref LayoutNode root)
    {
        var newTabGroup = new TabGroupNode();
        newTabGroup.Panes.Add(sourcePane);
        sourcePane.Parent = newTabGroup;
        newTabGroup.ActivePane = sourcePane;

        var orientation = (position == DockPosition.Left || position == DockPosition.Right)
            ? Orientation.Horizontal
            : Orientation.Vertical;

        var isNewFirst = (position == DockPosition.Left || position == DockPosition.Top);

        var newSplit = new SplitNode
        {
            Orientation = orientation,
            Ratio = 0.5,
            FirstChild = isNewFirst ? newTabGroup : targetNode,
            SecondChild = isNewFirst ? targetNode : newTabGroup,
            Parent = targetNode.Parent
        };

        newTabGroup.Parent = newSplit;

        if (targetNode.Parent == null) // targetNode was the root
        {
            targetNode.Parent = newSplit;
            root = newSplit;
        }
        else if (targetNode.Parent is SplitNode parentSplit)
        {
            if (parentSplit.FirstChild == targetNode)
            {
                parentSplit.FirstChild = newSplit;
            }
            else if (parentSplit.SecondChild == targetNode)
            {
                parentSplit.SecondChild = newSplit;
            }

            targetNode.Parent = newSplit;
        }
    }

    /// <summary>
    /// Recursively normalizes the tree enforcing structural invariants:
    /// 1. Prunes empty TabGroupNodes.
    /// 2. Collapses SplitNodes with missing children.
    /// </summary>
    public LayoutNode? NormalizeTree(LayoutNode? node)
    {
        if (node == null) return null;

        if (node is TabGroupNode tabGroup)
        {
            // Invariant 1: An empty TabGroup is invalid
            return tabGroup.Panes.Count == 0 ? null : tabGroup;
        }

        if (node is SplitNode split)
        {
            // Recursively normalize left and right subtrees
            split.FirstChild = NormalizeTree(split.FirstChild);
            split.SecondChild = NormalizeTree(split.SecondChild);

            // Re-assign parent pointers
            if (split.FirstChild != null) split.FirstChild.Parent = split;
            if (split.SecondChild != null) split.SecondChild.Parent = split;

            // Invariant 2: Split with no children is eliminated
            if (split.FirstChild == null && split.SecondChild == null)
            {
                return null;
            }

            // Invariant 3: Single-child split is collapsed to that child
            if (split.FirstChild == null)
            {
                split.SecondChild!.Parent = split.Parent;
                return split.SecondChild;
            }

            if (split.SecondChild == null)
            {
                split.FirstChild!.Parent = split.Parent;
                return split.FirstChild;
            }

            return split;
        }

        return node;
    }
}