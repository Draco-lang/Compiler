import * as d3 from "d3";
import React from "react";
import { MessageModel, ThreadModel, TraceModel } from "./Model";

type Props = {
    width: number;
    height: number;
    data: ThreadModel;
    startColor?: any;
    endColor?: any;
};

interface TimelineMessageModel extends MessageModel {
    isPlaceholder: boolean;
};

const TimelineGraph = (props: Props) => {
    const domRef = React.useRef(null);

    const [data, setData] = React.useState(props.data);

    React.useEffect(() => buildGraph(domRef, props), [data]);

    return (
        <svg ref={domRef}>
        </svg>
    );
};

function buildGraph(domRef: React.MutableRefObject<null>, props: Props) {
    const svg = d3
        .select(domRef.current)
        .attr('width', props.width)
        .attr('height', props.height)
        .attr('cursor', 'grab');

    const messageHierarchy = d3.hierarchy(toTimelineMessage(props.data.rootMessage), getTimelineChildren);
    messageHierarchy.sum(node => node.children && node.children.length > 0 ? 0 : getTimeSpan(node));

    const partitionLayout = d3
        .partition<TimelineMessageModel>()
        .size([props.width, props.height])
        .padding(2);

    let laidOutMessages = partitionLayout(messageHierarchy);

    const colorScale = d3.interpolateHsl(props.startColor || 'green', props.endColor || 'red');

    // Groups of rect and text
    const allGroups = svg
        .selectAll('g')
        .data(laidOutMessages.descendants())
        .enter()
        .append('g');

    // Rects
    const allRects = allGroups
        .append('rect')
        .attr('x', node => node.x0)
        .attr('y', node => props.height - node.y1)
        .attr('width', node => node.x1 - node.x0)
        .attr('height', node => node.y1 - node.y0)
        .attr('fill', node => {
            if (node.data.isPlaceholder) return 'transparent';
            const fillPercentage = node.parent
                ? getTimeSpan(node.data) / getTimeSpan(node.parent.data)
                : 1;
            return colorScale(fillPercentage);
        });

    // Texts
    const allTexts = allGroups
        .append('text')
        .text(node => node.data.name)
        .attr('color', 'black')
        .attr('dominant-baseline', 'middle')
        .attr('text-anchor', 'middle')
        .attr('x', node => node.x0 + (node.x1 - node.x0) / 2)
        .attr('y', node => props.height - node.y1 + (node.y1 - node.y0) / 2);

    const zoom = d3
        .zoom()
        .on('zoom', e => {
            const transition = d3
                .transition()
                .duration(100);
            const {k, x, y} = e.transform;
            svg
                .selectAll('g')
                .select('rect')
                .transition(transition)
                .attr('transform', `translate(${x} 0) scale(${k} 1)`);
            svg
                .selectAll('g')
                .select('text')
                .transition(transition)
                .attr('transform', (node: any) => {
                    let target = node.target || node;
                    return `translate(${x + (k - 1) * ((target.x0 + target.x1) / 2)} 0)`;
                });
        });

    svg.call(zoom as any);

    allRects.on('click', function (element, node) {
        focus(node);

        const transition = d3
            .transition()
            .duration(500);
        allRects
            .transition(transition)
            .attr('x', (node: any) => node.target.x0)
            .attr('width', (node: any) => node.target.x1 - node.target.x0);
        allTexts
            .transition(transition)
            .attr('x', (node: any) => node.target.x0 + (node.target.x1 - node.target.x0) / 2);
    });
}

function focus(node: d3.HierarchyRectangularNode<TimelineMessageModel>) {
    function cleanse(node: any) {
        (node as any).target = null;
        if (node.children) {
            for (let child of node.children) cleanse(child);
        }
    }

    function resize(node: d3.HierarchyRectangularNode<TimelineMessageModel>, x0: number, x1: number) {
        (node as any).target = {
            x0,
            y0: node.y0,
            x1,
            y1: node.y1,
        };
    }

    function scaleUpChild(node: d3.HierarchyRectangularNode<TimelineMessageModel>, parent: any) {
        const parentSpan = getTimeSpan(parent.data);
        const startPercent = (node.data.startTime - parent.data.startTime) / parentSpan;
        const endPercent = (node.data.endTime - parent.data.startTime) / parentSpan;
        const parentSize = parent.target.x1 - parent.target.x0;

        resize(node, parent.target.x0 + startPercent * parentSize, parent.target.x0 + endPercent * parentSize);

        if (node.children) {
            for (let child of node.children) scaleUpChild(child, node);
        }
    }

    // Find root
    let root = node;
    while (root.parent) root = root.parent;

    function collapseLeft(node: d3.HierarchyRectangularNode<TimelineMessageModel>) {
        resize(node, root.x0, root.x0);
        if (node.children) {
            for (let child of node.children) collapseLeft(child);
        }
    }

    function collapseRight(node: d3.HierarchyRectangularNode<TimelineMessageModel>) {
        resize(node, root.x1, root.x1);
        if (node.children) {
            for (let child of node.children) collapseRight(child);
        }
    }

    function collapseRemaining(node: any) {
        if (!node.children) return;

        let isLeft = true;
        for (let child of node.children) {
            if (child.target) {
                isLeft = false;
                collapseRemaining(child);
                continue;
            }

            if (isLeft) collapseLeft(child);
            else collapseRight(child);
        }
    }

    // Cleanse target transforms
    cleanse(root);

    // Walk up the ancestry chain, expand
    let current = node;
    while (current) {
        resize(current, root.x0, root.x1);
        current = current.parent!;
    }

    // Walk down, scale up children
    if (node.children) {
        for (let child of node.children) scaleUpChild(child, node);
    }

    // Collapse everything else
    collapseRemaining(root);
}

function getTimeSpan(msg: MessageModel): number {
    return msg.endTime - msg.startTime;
}

function toTimelineMessage(msg: MessageModel): TimelineMessageModel {
    return {
        ...msg,
        isPlaceholder: false,
    };
}

function getTimelineChildren(msg: MessageModel): TimelineMessageModel[] {
    function makePlaceholder(startTime: number, endTime: number): TimelineMessageModel {
        return {
            name: '',
            startTime,
            endTime,
            isPlaceholder: true,
        };
    }

    if (!msg.children || msg.children.length === 0) return [];

    const result = new Array<TimelineMessageModel>();

    // Check the gap between first child and parent
    if (msg.startTime < msg.children[0].startTime) {
        // Push placeholder
        result.push(makePlaceholder(msg.startTime, msg.children[0].startTime));
    }

    for (let i = 0; i < msg.children.length; ++i) {
        // Add current message
        const child = msg.children[i];
        result.push(toTimelineMessage(child));

        // Look at next message, if there is a gap, fill it
        if (i + 1 < msg.children.length) {
            const nextChild = msg.children[i + 1];
            if (child.endTime < nextChild.startTime) {
                // Push placeholder
                result.push({
                    name: '',
                    startTime: child.endTime,
                    endTime: nextChild.startTime,
                    isPlaceholder: true,
                });
            }
        }
    }

    // Check the gap between last child and parent
    if (msg.children[msg.children.length - 1].endTime < msg.endTime) {
        // Push placeholder
        result.push(makePlaceholder(msg.children[msg.children.length - 1].endTime, msg.endTime));
    }

    return result;
}

export default TimelineGraph;
