﻿using System.Collections.Generic;

namespace ENode.Eventing.Impl
{
    public interface ICommitLog
    {
        /// <summary>Append the given event stream to the log, and returns the commit sequence.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        long Append(EventStream stream);
        /// <summary>Get the event stream by the commit sequence.
        /// </summary>
        /// <param name="commitSequence"></param>
        /// <returns></returns>
        EventStream Get(long commitSequence);
        /// <summary>Query event streams by start sequence and size.
        /// </summary>
        /// <param name="startSequence"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IEnumerable<EventStream> Query(long startSequence, int size);
    }
}
