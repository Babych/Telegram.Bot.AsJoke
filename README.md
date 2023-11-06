# Pipelines status:
[![Docker Image Build and Deploy - devbotvm](https://github.com/Babych/Telegram.Bot.AsJoke/actions/workflows/docker-image.yml/badge.svg)](https://github.com/Babych/Telegram.Bot.AsJoke/actions/workflows/docker-image.yml)

# Telegram.Bot Examples

[![telegram chat](https://img.shields.io/badge/Support_Chat-Telegram-blue.svg?style=flat-square)](https://t.me/joinchat/B35YY0QbLfd034CFnvCtCA)
[![package](https://img.shields.io/nuget/vpre/Telegram.Bot.svg?label=Telegram.Bot&style=flat-square)](https://www.nuget.org/packages/Telegram.Bot)
[![documentations](https://img.shields.io/badge/Documentations-Book-orange.svg?style=flat-square)](https://telegrambots.github.io/book/)
[![master build status](https://img.shields.io/github/actions/workflow/status/TelegramBots/Telegram.Bot.Examples/build_examples.yml?style=flat-square)](https://github.com/TelegramBots/Telegram.Bot.Examples/actions/workflows/build_examples.yml)

## About

This repository contains sample applications based on [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) library:

- [Simple console application](https://github.com/TelegramBots/Telegram.Bot.Examples/tree/master/Telegram.Bot.Examples.Polling). Demonstrates use of [Telegram.Bot.Extensions.Polling](https://github.com/TelegramBots/Telegram.Bot.Extensions.Polling).
- [ASP.NET Core Web Hook application](https://github.com/TelegramBots/Telegram.Bot.Examples/tree/master/Telegram.Bot.Examples.WebHook).

### Comunity projects

- [GreenCaptchaBot](https://github.com/ImoutoChan/GreenCaptchaBot) a spam-protection bot. This is a bot that will ask the users coming to a Telegram chat to press a random numbered button.
- [vahter-bot](https://github.com/fsharplang-ru/vahter-bot) the bot that bans.

### Legacy projects

These projects represent older or deprecated technologies you might want to use, though we do not provide any support for those projects:

- [Classic ASP.NET MVC 5 application](https://github.com/TelegramBots/Telegram.Bot.Examples/tree/legacy-ASPNET).
- [Simple console application](https://github.com/TelegramBots/Telegram.Bot.Examples/tree/legacy-events). Based on build-in events system.

## Requirements

Examples in this repository use .NET 6. You might need to install required version from [here](https://dotnet.microsoft.com/download).

## Community

Feel free do join our [discussion group](https://t.me/tgbots_dotnet)!

docker stop:
docker stop $(docker ps -q --filter ancestor=dbabych/devbotvm:latest) || true
