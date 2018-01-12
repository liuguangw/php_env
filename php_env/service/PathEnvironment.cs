using System.Collections.Generic;

namespace php_env.service
{
    public static class PathEnvironment
    {
        public static List<string> getPathList(System.EnvironmentVariableTarget target)
        {
            List<string> pathList = new List<string>();
            string pathStr = System.Environment.GetEnvironmentVariable("Path", target);
            if (pathStr == null)
            {
                return pathList;
            }
            pathStr = pathStr.TrimEnd(';');
            if (pathStr.Length == 0)
            {
                return pathList;
            }
            string[] pathArray = pathStr.Split(';');
            foreach (string path in pathArray)
            {
                if (path.EndsWith("\\"))
                {
                    pathList.Add(path.TrimEnd('\\'));
                }
                else
                {
                    pathList.Add(path);
                }
            }
            return pathList;
        }

        public static void setPathList(List<string> pathList, System.EnvironmentVariableTarget target)
        {
            if (pathList.Count == 0)
            {
                return;
            }
            System.Environment.SetEnvironmentVariable("Path", System.String.Join(";", pathList) + ";", target);
        }
    }
}
