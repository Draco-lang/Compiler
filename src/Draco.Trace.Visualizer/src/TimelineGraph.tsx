import * as d3 from "d3";
import React from "react";
import { MessageModel, ThreadModel, TraceModel } from "./Model";

type Props = {
    width: number;
    height: number;
    data: ThreadModel;
};

interface TimelineMessageModel extends MessageModel {
    isPlaceholder: boolean;
};

const TimelineGraph = (props: Props) => {
    const domRef = React.useRef(null);

    const [data, setData] = React.useState(props.data);
    const [width, setWidth] = React.useState(props.width);
    const [height, setHeight] = React.useState(props.height);

    React.useEffect(() => {
        const svg = d3
            .select(domRef.current)
            .attr('width', width)
            .attr('height', height);

        const messageHierarchy = d3.hierarchy(toTimelineMessage(data.rootMessage), getTimelineChildren);
        messageHierarchy.sum(msg => msg.endTime - msg.startTime);

        const partitionLayout = d3
            .partition<TimelineMessageModel>()
            .size([width, height])
            .padding(2);

        partitionLayout(messageHierarchy);

        const colorScale = d3.interpolateHsl('green', 'red');

        svg
            .selectAll('rect')
            .data(messageHierarchy.descendants())
            .join('rect')
            .attr('x', node => (node as any).x0)
            .attr('y', node => height - (node as any).y1)
            .attr('width', node => (node as any).x1 - (node as any).x0)
            .attr('height', node => (node as any).y1 - (node as any).y0)
            .attr('fill', node => {
                if (node.data.isPlaceholder) return 'transparent';
                const fillPercentage = node.parent
                    ? getTimeSpan(node.data) / getTimeSpan(node.parent.data)
                    : 1;
                return colorScale(fillPercentage);
            });
    }, [data, width, height]);

    return (
        <svg ref={domRef}>
        </svg>
    );
};

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
    if (!msg.children || msg.children.length === 0) return [];

    const result = [];

    // Check the gap between first child and parent
    if (msg.startTime < msg.children[0].startTime) {
        // Push placeholder
        result.push({
            name: '',
            startTime: msg.startTime,
            endTime: msg.children[0].startTime,
            isPlaceholder: true,
        });
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

    return result;
}

export default TimelineGraph;
