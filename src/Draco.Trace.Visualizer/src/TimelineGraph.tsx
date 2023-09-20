import * as d3 from "d3";
import * as d3scale from "d3-scale";
import React from "react";

const TimelineGraph = () => {
    const domRef = React.useRef(null);

    const [data, setData] = React.useState([
        { x: 10, y: 10, w: 50, h: 10 },
        { x: 10, y: 30, w: 70, h: 10 },
        { x: 10, y: 50, w: 20, h: 10 },
        { x: 10, y: 70, w: 30, h: 10 },
    ]);

    React.useEffect(() => {
        const svg = d3.select(domRef.current);

        svg
            .selectAll('rect')
            .data(data)
            .enter()
            .append('rect')
            .attr('x', data => data.x)
            .attr('y', data => data.y)
            .attr('width', data => data.w)
            .attr('height', data => data.h);
    }, [data])

    return (
        <svg ref={domRef}>
        </svg>
    );
};

export default TimelineGraph;
