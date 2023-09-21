/**
 * Represents a bounding box.
 */
export interface BoundingBox {
    /**
     * The left of the bounding box.
     */
    x0: number;

    /**
     * The top of the bounding box.
     */
    y0: number;

    /**
     * The right of the bounding box.
     */
    x1: number;

    /**
     * The bottom of the bounding box.
     */
    y1: number;
}

/**
 * Represents a single node laid out in a hierarchical bar graph.
 * @template TData The associated node data type.
 */
export interface HierarchicalBarGraphNode<TData> {
    /**
     * The underlying data for this node.
     */
    data: TData;

    /**
     * The parent node of this one.
     */
    parent: HierarchicalBarGraphNode<TData> | undefined;

    /**
     * The immediate child nodes of this one.
     */
    children: Iterable<HierarchicalBarGraphNode<TData>>;

    /**
     * The bounds of this node.
     */
    bounds: BoundingBox;

    /**
     * The bounds that should be used for rendering.
     */
    visualBounds: BoundingBox;
}

/**
 * The settings to use for a timeline layout.
 * @template TData The associated data type for the nodes.
 */
export type TimelineLayoutSettings<TData> = {
    /**
     * The available size for the graph.
     */
    size: [width: number, height: number];

    /**
     * The padding to use between graph bars.
     */
    padding?: [horizontal: number, vertical: number];

    /**
     * The desired bar height.
     */
    barHeight?: number;

    /**
     * The function to retrieve the children of the given node.
     * @param node The node to retrieve the children for.
     * @returns The children of @param node.
     */
    getChildren: (node: TData) => Iterable<TData>;

    /**
     * The function to retrieve the range that @param node should fill out.
     * @param node The node to retrieve the range for.
     * @param parent The parent node of this one, if it's not root.
     * @returns The relative range (in percentage) that @param node occupies relative to @param parent.
     */
    getRange: (node: TData, parent: TData | undefined) => [start: number, end: number];
};

/**
 * Lays out a timeline graph starting with the given root.
 * @param root The root node to start the layout from.
 * @param settings The settings to use for the layout.
 */
export function layoutTimeline<TData>(root: TData, settings: TimelineLayoutSettings<TData>): HierarchicalBarGraphNode<TData> {
    const [width, height] = settings.size;
    const horizontalPadding = settings.padding?.[0] ?? 0;
    const horizontalPadding2 = horizontalPadding / 2;
    const verticalPadding = settings.padding?.[1] ?? 0;
    // TODO: proper default height
    // Assuming
    //  D: max tree depth
    //  P: vertical padding
    //  H: graph height
    //  B: default bar height
    // Then
    //  B = (H - D * P + P) / D
    const barHeight = settings.barHeight ?? 0;

    function layoutTimelineImpl(current: TData, parent: HierarchicalBarGraphNode<TData> | undefined, yOffset: number, xOffset: [x0: number, x1: number]): HierarchicalBarGraphNode<TData> {
        const availableSpace = xOffset[1] - xOffset[0];
        const [startPercentage, endPercentage] = settings.getRange(current, parent?.data);

        const children = new Array<HierarchicalBarGraphNode<TData>>();
        const bounds: BoundingBox = {
            x0: xOffset[0] + startPercentage * availableSpace + horizontalPadding2,
            x1: xOffset[0] + endPercentage * availableSpace - horizontalPadding2,
            y0: yOffset - barHeight,
            y1: yOffset,
        };
        const node: HierarchicalBarGraphNode<TData> = {
            data: current,
            parent: parent,
            bounds,
            visualBounds: { ...bounds },
            children,
        };

        for (let child of settings.getChildren(current)) {
            const childNode = layoutTimelineImpl(child, node, yOffset - barHeight - verticalPadding, [node.bounds.x0, node.bounds.x1]);
            children.push(childNode);
        }

        return node;
    }

    return layoutTimelineImpl(root, undefined, height, [0, width]);
}

function clearVisualBounds<TData>(node: HierarchicalBarGraphNode<TData>) {
    (node as any).visualBounds = undefined;
    for (let child of node.children) clearVisualBounds(child);
}

function focusVisualsOnNode<TData>(node: HierarchicalBarGraphNode<TData>) {
    function resize(node: HierarchicalBarGraphNode<TData>, x0: number, x1: number) {
        const sourceBounds = node.visualBounds ?? node.bounds;
        node.visualBounds = {
            x0,
            y0: sourceBounds.y0,
            x1,
            y1: sourceBounds.y1,
        };
    }

    function scaleUpChild(node: HierarchicalBarGraphNode<TData>, parent: HierarchicalBarGraphNode<TData>) {
        const parentSpan = parent.bounds.x1 - parent.bounds.x0;
        const startPercent = (node.bounds.x0 - parent.bounds.x0) / parentSpan;
        const endPercent = (node.bounds.x1 - parent.bounds.x0) / parentSpan;
        const parentSize = parent.visualBounds.x1 - parent.visualBounds.x0;

        resize(node, parent.visualBounds.x0 + startPercent * parentSize, parent.visualBounds.x0 + endPercent * parentSize);

        if (node.children) {
            for (let child of node.children) scaleUpChild(child, node);
        }
    }

    // Find root
    let root = node;
    while (root.parent) root = root.parent;

    function collapseLeft(node: HierarchicalBarGraphNode<TData>) {
        resize(node, root.bounds.x0, root.bounds.x0);
        if (node.children) {
            for (let child of node.children) collapseLeft(child);
        }
    }

    function collapseRight(node: HierarchicalBarGraphNode<TData>) {
        resize(node, root.visualBounds.x1, root.visualBounds.x1);
        if (node.children) {
            for (let child of node.children) collapseRight(child);
        }
    }

    function collapseRemaining(node: HierarchicalBarGraphNode<TData>) {
        if (!node.children) return;

        let isLeft = true;
        for (let child of node.children) {
            if (child.visualBounds) {
                isLeft = false;
                collapseRemaining(child);
                continue;
            }

            if (isLeft) collapseLeft(child);
            else collapseRight(child);
        }
    }

    // Cleanse visual transforms
    clearVisualBounds(root);

    // Walk up the ancestry chain, expand
    let current = node;
    while (current) {
        resize(current, root.bounds.x0, root.bounds.x1);
        current = current.parent!;
    }

    // Walk down, scale up children
    if (node.children) {
        for (let child of node.children) scaleUpChild(child, node);
    }

    // Collapse everything else
    collapseRemaining(root);
}
