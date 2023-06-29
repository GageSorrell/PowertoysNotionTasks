# NotionTask Plugin for PowerToys Run

This is a plugin for [PowerToys Run](https://github.com/microsoft/PowerToys/wiki/PowerToys-Run-Overview) that allows you to quickly add tasks to a task list in Notion by specifying a title, then adding other details in the browser.

## Demo

@TODO

## Features

* Adds a task to Notion as a page in a database, such that the title is the task name
* Specify an icon and banner for the resulting Notion page
* *Optionally,* immediately open the page in the default browser

## Installation

1. Download the latest release of the NotionTask Plugin from the [releases page](https://github.com/GageSorrell/PowertoysNotionTask/releases).
2. Extract the contents of the zip file to your PowerToys modules directory (usually `C:\Program Files\PowerToys\modules\launcher\Plugins`).
3. Configure per the following section, then restart Powertoys.

## Configuration

1. Create a [Notion integration](https://www.notion.so/my-integrations) and copy the secret
2. Create a database for your task list (or use an existing one), and share it with your integration via the dropdown in the upper-right corner
3. Open `Properties\Resources.resx` in your text editor
4. Replace `YOUR_INTEGRATION_SECRET` with the secret that you copied earlier
5. Copy your database's ID and replace `YOUR_DATABASE_ID` with your ID
6. *Optionally,* set URLs for banner and icon images, and modify the sequences that trigger the plugin

## Usage

1. Open PowerToys Run (default shortcut is `Alt+Space`).
2. Type `;` or `;;` and your task  followed by your search query.
3. Select a package from the search results and press `Enter` to install it.

## Build

1. Clone the [PowerToys repo](https://github.com/microsoft/PowerToys).
2. cd into the `PowerToys` directory.
3. Initialize the submodules: `git submodule update --init --recursive`
4. Clone this repo into the `PowerToys/src/modules/launcher/Plugins` directory. (`git clone https://github.com/GageSorrell/PowertoysNotionTask PowerToys/src/modules/launcher/Plugins/Community.PowerToys.Run.Plugin.NotionTask`)
5. Open the `PowerToys.sln` solution in Visual Studio.
6. Add this project to the `PowerToys.sln` solution. (Right-click on the `PowerToys` solution in the Solution Explorer (under the path PowerToys/src/modules/launcher/Plugins) and select `Add > Existing Project...` and select the `Community.PowerToys.Run.Plugin.NotionTask.csproj` file.)
7. Build the solution.
8. Run the `PowerToys` project.

## License

This project is licensed under the [MIT License](LICENSE).