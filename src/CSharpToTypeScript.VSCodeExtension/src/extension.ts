import * as cp from 'child_process';
import * as path from 'path';
import * as readline from 'readline';
import * as vscode from 'vscode';
import { dateOutputTypes, Input, nullableOutputTypes, quotationMarks } from './input';
import { Output } from './output';
import { allowedOrDefault, fullRange, getTextFromActiveDocument } from './utilities';

let server: cp.ChildProcess | undefined;
let rl: readline.Interface | undefined;
let serverRunning = false;
let executingCommand = false;

export function activate(context: vscode.ExtensionContext) {
    let standardError = '';
    serverRunning = true;

    server = cp.spawn('dotnet', [context.asAbsolutePath(path.join(
        'server', 'CSharpToTypeScript.Server', 'bin', 'Release', 'netcoreapp2.2', 'publish', 'CSharpToTypeScript.Server.dll'))]);

    server.on('error', err => {
        serverRunning = false;
        vscode.window.showErrorMessage(`"C# to TypeScript" server related error occurred: "${err.message}".`);
    });
    server.stderr.on('data', data => {
        standardError += data;
    });
    server.on('exit', code => {
        serverRunning = false;
        vscode.window.showWarningMessage(`"C# to TypeScript" server shutdown with code: "${code}". Standard error: "${standardError}".`);
    });

    rl = readline.createInterface(server.stdout, server.stdin);

    context.subscriptions.push(
        vscode.commands.registerCommand('csharpToTypeScript.csharpToTypeScriptReplace', replaceCommand),
        vscode.commands.registerCommand('csharpToTypeScript.csharpToTypeScriptToClipboard', toClipboardCommand),
        vscode.commands.registerCommand('csharpToTypeScript.csharpToTypeScriptPasteAs', paseAsCommand));
}

export function deactivate() {
    if (serverRunning && server) {
        server.stdin.write('EXIT\n');
    }
}

async function replaceCommand() {
    const code = getTextFromActiveDocument();

    await convert(code, async convertedCode => {
        if (!vscode.window.activeTextEditor) {
            return;
        }

        const document = vscode.window.activeTextEditor.document;
        const selection = vscode.window.activeTextEditor.selection;

        await vscode.window.activeTextEditor.edit(
            builder => builder.replace(!selection.isEmpty ? selection : fullRange(document), convertedCode));
    });
}

async function toClipboardCommand() {
    const code = getTextFromActiveDocument();

    await convert(code, async convertedCode => {
        await vscode.env.clipboard.writeText(convertedCode);
    });
}

async function paseAsCommand() {
    const code = await vscode.env.clipboard.readText();

    await convert(code, async convertedCode => {
        if (!vscode.window.activeTextEditor) {
            return;
        }

        const selection = vscode.window.activeTextEditor.selection;

        await vscode.window.activeTextEditor.edit(
            builder => builder.replace(selection, convertedCode));
    });
}

export async function convert(code: string, onConverted: (convertedCode: string) => Promise<void>) {
    if (!serverRunning) {
        vscode.window.showErrorMessage(`"C# to TypeScript" server isn't running! Reload Window to restart it.`);
        return;
    }

    if (!vscode.window.activeTextEditor || !rl || executingCommand) {
        return;
    }

    executingCommand = true;

    const configuration = vscode.workspace.getConfiguration();

    const input: Input = {
        code: code,
        useTabs: !vscode.window.activeTextEditor.options.insertSpaces,
        tabSize: vscode.window.activeTextEditor.options.tabSize as number,
        export: !!configuration.get('csharpToTypeScript.export'),
        convertDatesTo: allowedOrDefault(configuration.get('csharpToTypeScript.convertDatesTo'), dateOutputTypes),
        convertNullablesTo: allowedOrDefault(configuration.get('csharpToTypeScript.convertNullablesTo'), nullableOutputTypes),
        toCamelCase: !!configuration.get('csharpToTypeScript.toCamelCase'),
        removeInterfacePrefix: !!configuration.get('csharpToTypeScript.removeInterfacePrefix'),
        generateImports: !!configuration.get('csharpToTypeScript.generateImports'),
        useKebabCase: !!configuration.get('csharpToTypeScript.useKebabCase'),
        appendModelSuffix: !!configuration.get('csharpToTypeScript.appendModelSuffix'),
        quotationMark: allowedOrDefault(configuration.get('csharpToTypeScript.quotationMark'), quotationMarks)
    };

    const inputLine = JSON.stringify(input) + '\n';

    rl.question(inputLine, async outputLine => {
        const { convertedCode, succeeded, errorMessage } = JSON.parse(outputLine) as Output;

        if (!succeeded) {
            if (errorMessage) {
                vscode.window.showErrorMessage(`"C# to TypeScript" extension encountered an error while converting your code: "${errorMessage}".`);
            } else {
                vscode.window.showErrorMessage(`"C# to TypeScript" extension encountered an unknown error while converting your code.`);
            }
        } else if (!convertedCode) {
            vscode.window.showWarningMessage(`Nothing to convert - C# to TypeScript conversion resulted in an empty string.`);
        } else {
            await onConverted(convertedCode);
        }

        executingCommand = false;
    });
}

