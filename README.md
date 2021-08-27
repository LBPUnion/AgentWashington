# Agent Washington

Agent Washington is an open-source, open-minded LittleBigPlanet server monitoring tool. It is written in C# and is designed to be easily readable and extendable. It only monitors the game servers that are actually important, and is also aimed to cause as little confusion and spread of misinformation as possible in the game's community.

In its current form, this tool monitors the main game server port and sends an HTTP request to it. If the request succeeds, the response is written to the console. Otherwise, the status code and error text is written, and an explanation of what this means in the context of LBP is also written.

In the future, this will be a Discord bot for the [LBP Union](https://lbpunion.com/) Discord server. Once this is set up, any LBP community server is welcomed to invite this bot.

## Help wanted!

If you know more servers we should be monitoring, or any other ways we should monitor them, submit a GitHub issue and we will look into it.

Also, if there are any other known status codes the server responds with, and you know what they mean in the context of LBP, please add them to the `GetStatusInfo()` method and send a pull request. We'll verify it and merge it.

## FAQ

Here are some common questions we may be asked and here are the answers.

### Why are you not monitoring the Image CDN?

Because it's down, and it's intentionally down. The image CDN is not required for the game's online features to function.

### Vita servers?

We don't know if the Vita servers are coming back, and so we're not sure if they should be still monitored. If information on them changes, we'll get that sorted out.

### LBP.me

See "Vita servers?"

### Port 10060

Port 10060 is insecure and, like the Image CDN, is obsolete. Port 10061 is what the game connects to straight after authenticating with the PlayStation Network. If a connection to port 10061 can't be made, the game shows an error message. `GetStatusInfo()` describes how the game's various messages occur.

### You're blindly trusting the gameserver's SSL certificate without even checking the certificate chain. Why?

Because we're not a PlayStation. YOU WILL ALWAYS get an SSL error trying to connect to the LBP servers unless you are actually a copy of LittleBigPlanet. Short of ripping the root CA off of an LBP copy, the only way to get around this is to blindly trust the certificate.

### Why are you doing a DNS lookup of the gameserver domain when you just use a normal URL to make the HTTP request?

It's very difficult to reliably tell if you're connected to the Internet, doing a DNS lookup is a very standard and simple way to do so. If we can't find the server's IP address with a DNS lookup then either one of two things happened.

1. You're not connected to the Internet.
2. You are, but your primary and secondary DNS servers are down.

In either case, you won't be connecting to LBP servers even if they are online.

### Isn't it bad to have the domain of the game server public like this?

No, it's about as dangerous as knowing that the domain name for Google is google.com. It's very easy to find the domain with Wireshark or `tcpdump`, there's no sense keeping it a secret - it won't stop a DDoSer or hacker from finding it anyway.

### Why is this open-source?

Because all my code is. You should always vet this kind of code before you run it, and this allows you to compile it yourself and know for certain that it's reliable. It also allows more knowledgeable community members to contribute to the code and make this tool better.

## Credit where credit's due

 - **Michael Youngling**, the other Michael in all this. He's been a huge help in spreading my findings in a way people can easily understand without knowing how to code. He helps present correct information to the community.
 - **MysteriousCube**, for giving more detailed insight into the game server situation and how the game's engine works.

## Build instructions

You will need the .NET 5 SDK for your platform. Once you have it, clone the repository.

```bash
git clone https://github.com/alkalinethunder/CraftworldMonitor
```

Then navigate into the project directory

```bash
cd src/AlkalineThunder.CraftworldMonitor
```

And build the project

```bash
dotnet build
```

You can run it with

```bash
dotnet run
```

## License

As with many pf my projects, this code's licensed under the MIT license. This basically means that you can use it however you want as long as you give me credit and the MIT license and copyright text is _somewhere_ I can find in your code.  See [LICENSE](/LICENSE) for details.
