package io.github.ldev22.screen;

import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.Screen;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.graphics.Texture;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import io.github.ldev22.utils.GdxGraphics;

public class GameScreen implements Screen {

    private SpriteBatch batch;
    private Texture img;
    @Override
    public void show() {
        batch = new SpriteBatch();
        img = new Texture(Gdx.files.internal("raw/background-blue.png"));
    }

    @Override
    public void render(float delta) {
        GdxGraphics.clearScreen(Color.BLACK);

        batch.begin();
        batch.draw(img, 0.0f, 0.0f);
        batch.end();
    }

    @Override
    public void resize(int width, int height) {

    }

    @Override
    public void pause() {

    }

    @Override
    public void resume() {

    }

    @Override
    public void hide() {

    }

    @Override
    public void dispose() {
        batch.dispose();
        img.dispose();
    }
}
