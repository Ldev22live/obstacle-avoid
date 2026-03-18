package io.github.ldev22;

import com.badlogic.gdx.ApplicationAdapter;
import com.badlogic.gdx.Game;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.badlogic.gdx.utils.Logger;
import com.badlogic.gdx.utils.ScreenUtils;
import io.github.ldev22.screen.GameScreen;

/** {@link com.badlogic.gdx.ApplicationListener} implementation shared by all platforms. */
public class ObstacleAvoidGame extends Game {
    public static Logger log = new Logger("GDX-DEBUG", Logger.DEBUG);
    @Override
    public void create() {
        setScreen(new GameScreen());
    }
}
