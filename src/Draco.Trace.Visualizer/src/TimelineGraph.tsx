import * as d3 from "d3";
import React from "react";
import { MessageModel, ThreadModel, TraceModel } from "./Model";
import { HierarchicalBarGraphNode, TimelineLayoutSettings, focusVisualsOnNode, layoutTimeline } from "./graph_utils";

type Props = {
    width: number;
    height: number;
    data: ThreadModel;
    startColor?: any;
    endColor?: any;
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

    const layoutSettings: TimelineLayoutSettings<MessageModel> = {
        size: [props.width, props.height],
        barHeight: 50,
        padding: [2, 2],
        getChildren: (node) => node.children ?? [],
        getRange: (node, parent) => {
            if (!parent) return [0, 1];

            const parentSpan = parent.endTime - parent.startTime;
            const startPercentage = (node.startTime - parent.startTime) / parentSpan;
            const endPercentage = (node.endTime - parent.startTime) / parentSpan;
            return [startPercentage, endPercentage];
        },
    };
    const laidOutMessages = layoutTimeline(props.data.rootMessage, layoutSettings);

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
        .attr('x', node => node.visualBounds.x0)
        .attr('y', node => props.height - node.visualBounds.y1)
        .attr('width', node => node.visualBounds.x1 - node.visualBounds.x0)
        .attr('height', node => node.visualBounds.y1 - node.visualBounds.y0)
        .attr('fill', node => {
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
        .attr('x', node => node.visualBounds.x0 + (node.visualBounds.x1 - node.visualBounds.x0) / 2)
        .attr('y', node => props.height - node.visualBounds.y1 + (node.visualBounds.y1 - node.visualBounds.y0) / 2);

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
                    return `translate(${x + (k - 1) * ((node.visualBounds.x0 + node.visualBounds.x1) / 2)} 0)`;
                });
        });

    svg.call(zoom as any);

    let lastFocused: HierarchicalBarGraphNode<MessageModel> | undefined = undefined;
    allRects.on('click', function (element, node) {
        if (lastFocused === node) {
            // Focus on root
            focusVisualsOnNode(laidOutMessages.root);
            lastFocused = undefined;
        }
        else {
            focusVisualsOnNode(node);
            lastFocused = node;
        }

        const transition = d3
            .transition()
            .duration(500);
        allRects
            .transition(transition)
            .attr('x', (node: any) => node.visualBounds.x0)
            .attr('width', (node: any) => node.visualBounds.x1 - node.visualBounds.x0);
        allTexts
            .transition(transition)
            .attr('x', (node: any) => node.visualBounds.x0 + (node.visualBounds.x1 - node.visualBounds.x0) / 2);
    });
}

function getTimeSpan(msg: MessageModel): number {
    return msg.endTime - msg.startTime;
}

export default TimelineGraph;
