import React from 'react';
import TimelineGraph from './TimelineGraph'
import { TraceModel } from './Model';

const sampleData: TraceModel = [
  {
    threadId: '0',
    rootMessage: {
      name: 'root',
      startTime: 0,
      endTime: 100,
      children: [
        {
          name: 'foo',
          startTime: 3,
          endTime: 20,
        },
        {
          name: 'bar',
          startTime: 30,
          endTime: 90,
          children: [
            {
              name: 'baz',
              startTime: 40,
              endTime: 80,
            }
          ]
        }
      ],
    },
  }
];

function App() {
  return (
    <TimelineGraph data={sampleData[0]} width={800} height={200} />
  );
}

export default App;
