public static class TerminalFileSystem
{
    public static FileSystemNode BuildFileSystem()
    {
        var root = new FileSystemNode("/", true);

        var home = new FileSystemNode("home", true, parent: root);
        root.Children.Add(home);

        var user = new FileSystemNode("user", true, parent: home);
        home.Children.Add(user);

        var docs = new FileSystemNode("documents", true, parent: user);
        user.Children.Add(docs);

        var secret = new FileSystemNode("secret", true, parent: user);
        user.Children.Add(secret);

        docs.Children.Add(new FileSystemNode("readme.txt", false, "Nothing interesting here.", docs));
        docs.Children.Add(new FileSystemNode("notes.txt", false, "Buy milk. Check emails.", docs));

        var hidden = new FileSystemNode(".hidden", true, parent: secret);
        secret.Children.Add(hidden);
        secret.Children.Add(new FileSystemNode("decoy.txt", false, "Nice try. Keep looking.", secret));

        hidden.Children.Add(new FileSystemNode("flag.txt", false, "FLAG{y0u_f0und_1t_g00d_j0b}", hidden));

        var etc = new FileSystemNode("etc", true, parent: root);
        root.Children.Add(etc);
        etc.Children.Add(new FileSystemNode("passwd", false, "root:x:0:0:root:/root:/bin/bash\nuser:x:1000:1000::/home/user:/bin/sh", etc));

        return root;
    }
}