using UnityEditor.PackageManager.Requests;

namespace Unity.ContentManager
{
    /// <summary>
    /// Contains data for a queued Package Manager operation.
    /// </summary>
    [System.Serializable]
    class ContentEvent
    {
        public PackageAction action;
        public ContentPack targetPack;
        public Request request;

        bool m_Complete;

        public bool EventComplete
        {
            get
            {
                if (request != null)
                    return request.IsCompleted;

                return m_Complete;
            }
            set
            {
                m_Complete = value;
            }
        }
    }
}
