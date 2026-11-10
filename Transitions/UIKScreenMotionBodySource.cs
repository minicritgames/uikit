using System.Collections.Generic;

namespace UIKit
{
    public interface UIKScreenMotionBodySource
    {
        public IReadOnlyList<UIKScreenMotionBody> GetMotionBodies(string _group);
        public IReadOnlyList<UIKScreenMotionBody> GetAllMotionBodies();
    }
} // UIKit namespace
