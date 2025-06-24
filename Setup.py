import os

# Create folder structure for C# AutoTrader project
base_path = "/mnt/data/AutoTrader"

folders = [
    "Questrade/Authentication",
    "Questrade/Market",
    "Questrade/Account",
    "Questrade/Orders",
    "Strategy",
    "Models",
    "Utils"
]

# Create folders
for folder in folders:
    os.makedirs(os.path.join(base_path, folder), exist_ok=True)

# Create main Program.cs file
program_cs_content = """
using System;

namespace AutoTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to AutoTrader!");
            // Entry point for authentication, market data, strategy, and order execution
        }
    }
}
"""

with open(os.path.join(base_path, "Program.cs"), "w") as f:
    f.write(program_cs_content)

# Create a sample appsettings.json for storing tokens/configs
appsettings_content = """
{
  "RefreshToken": "YOUR_REFRESH_TOKEN",
  "IsPractice": true
}
"""

with open(os.path.join(base_path, "appsettings.json"), "w") as f:
    f.write(appsettings_content)

# Display the created structure
import pandas as pd

project_structure = []
for root, dirs, files in os.walk(base_path):
    for name in files:
        project_structure.append([os.path.relpath(os.path.join(root, name), base_path)])

import ace_tools as tools; tools.display_dataframe_to_user(name="AutoTrader Project Structure", dataframe=pd.DataFrame(project_structure, columns=["File Path"]))
