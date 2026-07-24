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
    public void Dock(PaneNode sourcePane, LayoutNode targetNode, DockPosition position, ref LayoutNode root)
    {
        if (position == DockPosition.Center)
        {
            if (targetNode is TabGroupNode targetGroup)
            {
                targetGroup.AddPane(sourcePane); // Uses your clean AddPane method!
                targetGroup.ActivePane = sourcePane;
            }
            else
            {
                throw new InvalidOperationException("Cannot dock to 'Center' of a non-TabGroup node.");
            }
        }
        else
        {
            // Directional splits (Left, Right, Top, Bottom)
            SplitAndInsert(sourcePane, targetNode, position, ref root);
        }
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

        // Save targetNode's old parent before modifying pointers
        var oldParent = targetNode.Parent;

        var newSplit = new SplitNode
        {
            Orientation = orientation,
            Ratio = 0.5,
            FirstChild = isNewFirst ? newTabGroup : targetNode,
            SecondChild = isNewFirst ? targetNode : newTabGroup,
            Parent = oldParent
        };

        // Update parent pointers of children
        newTabGroup.Parent = newSplit;
        targetNode.Parent = newSplit;

        // Replace targetNode in oldParent with newSplit
        if (oldParent == null) // targetNode WAS the root node
        {
            root = newSplit;
        }
        else if (oldParent is SplitNode parentSplit)
        {
            if (parentSplit.FirstChild == targetNode)
            {
                parentSplit.FirstChild = newSplit;
            }
            else if (parentSplit.SecondChild == targetNode)
            {
                parentSplit.SecondChild = newSplit;
            }
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
    
    public void RelocatePane(PaneNode sourcePane, TabGroupNode targetGroup, DockPosition position, ref LayoutNode? root)
    {
        if (root == null) return;

        // 1. Remember the original TabGroupNode before removing sourcePane
        var sourceGroup = sourcePane.Parent as TabGroupNode 
                          ?? FindTabGroupContaining(root, sourcePane);

        if (sourceGroup == null) return;

        // 2. Remove the pane from the source group
        sourceGroup.Panes.Remove(sourcePane);

        if (sourceGroup.ActivePane == sourcePane)
        {
            sourceGroup.ActivePane = sourceGroup.Panes.LastOrDefault();
        }

        // 3. Dock the pane into the target destination
        Dock(sourcePane, targetGroup, position, ref root);

        // 4. Normalize the original source group if it was emptied out
        if (sourceGroup.Panes.Count == 0)
        {
            root = RemoveNodeAndNormalize(sourceGroup, root);
        }
    }
    
    /// <summary>
    /// Recursively searches the tree starting from <paramref name="root"/> 
    /// to locate the <see cref="TabGroupNode"/> containing the specified <paramref name="pane"/>.
    /// </summary>
    public TabGroupNode? FindTabGroupContaining(LayoutNode? root, PaneNode pane)
    {
        if (root == null) return null;

        if (root is TabGroupNode tabGroup)
        {
            if (tabGroup.Panes.Contains(pane))
            {
                return tabGroup;
            }
        }
        else if (root is SplitNode splitNode)
        {
            var foundInFirst = FindTabGroupContaining(splitNode.FirstChild, pane);
            if (foundInFirst != null) return foundInFirst;

            return FindTabGroupContaining(splitNode.SecondChild, pane);
        }

        return null;
    }

    /// <summary>
    /// Removes <paramref name="nodeToRemove"/> from the layout tree and normalizes the tree 
    /// by promoting sibling nodes to eliminate empty or single-child <see cref="SplitNode"/> containers.
    /// </summary>
    public LayoutNode? RemoveNodeAndNormalize(LayoutNode nodeToRemove, LayoutNode? root)
    {
        // Case 1: The node being removed is the root itself
        if (root == null || nodeToRemove == root)
        {
            return null;
        }

        // Resolve parent SplitNode
        var parent = nodeToRemove.Parent as SplitNode ?? FindParentSplitNode(root, nodeToRemove);
        if (parent == null)
        {
            return root;
        }

        // Determine the sibling node that should survive
        LayoutNode? survivingSibling = null;
        if (parent.FirstChild == nodeToRemove)
        {
            survivingSibling = parent.SecondChild;
        }
        else if (parent.SecondChild == nodeToRemove)
        {
            survivingSibling = parent.FirstChild;
        }

        if (survivingSibling == null)
        {
            return root;
        }

        // Promote the surviving sibling to take the place of the parent SplitNode
        var grandParent = parent.Parent as SplitNode ?? FindParentSplitNode(root, parent);

        if (grandParent != null)
        {
            survivingSibling.Parent = grandParent;

            if (grandParent.FirstChild == parent)
            {
                grandParent.FirstChild = survivingSibling;
            }
            else if (grandParent.SecondChild == parent)
            {
                grandParent.SecondChild = survivingSibling;
            }

            return root;
        }
        else
        {
            // Parent was the root; surviving sibling becomes the new root
            survivingSibling.Parent = null;
            return survivingSibling;
        }
    }

    /// <summary>
    /// Helper to find the parent SplitNode of a given target child if parent pointers are unassigned.
    /// </summary>
    private SplitNode? FindParentSplitNode(LayoutNode? root, LayoutNode targetChild)
    {
        if (root is not SplitNode splitNode) return null;

        if (splitNode.FirstChild == targetChild || splitNode.SecondChild == targetChild)
        {
            return splitNode;
        }

        return FindParentSplitNode(splitNode.FirstChild, targetChild) 
            ?? FindParentSplitNode(splitNode.SecondChild, targetChild);
    }

    public LayoutNode? FindLayoutNode()
    {
        // What parameter do we need here to clearly identify a PaneNode or rather assign it to a sidebar button?
        return null;
    }
}